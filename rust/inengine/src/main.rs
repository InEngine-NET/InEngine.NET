mod cli;
mod plugins;

use std::future::Future;
use std::io::IsTerminal;
use std::path::Path;
use std::process::ExitCode;
use std::sync::Arc;
use std::time::Duration;

use chrono::Utc;
use clap::{CommandFactory, Parser};
use tracing_subscriber::EnvFilter;

use inengine_core::command::{Command, CommandContext, CommandResult};
use inengine_core::commands::echo::Echo;
use inengine_core::commands::exec::Exec;
use inengine_core::config;
use inengine_core::error::describe_error_chain;
use inengine_core::output::Output;
use inengine_core::queue::QueueLane;
use inengine_core::queue_admin::management::{
    write_lengths, Clear, Peek, Recover, RepublishFailed,
};
use inengine_core::queue_admin::messaging::{Consume, Publish};
use inengine_core::server::host::Server;

use cli::{Cli, CliCommand, QueueSubcommand, ScheduleSubcommand};
use plugins::{
    load_plugins, log_plugin_errors, registry_with_plugins, render_schedule, run_plugin_command,
    schedule_with_plugins,
};

/// How long the runtime waits at shutdown for blocking tasks that are still running, such as
/// a command that ignores its deadline.
const RUNTIME_SHUTDOWN_LIMIT: Duration = Duration::from_secs(10);

const RUNTIME_START_FAILURE: &str = "could not start the async runtime";

/// Runs `future` on a new multi-thread runtime and shuts the runtime down when it ends.
fn run_async<F: Future>(future: F) -> std::io::Result<F::Output> {
    let runtime = tokio::runtime::Builder::new_multi_thread()
        .enable_all()
        .build()?;
    let output = runtime.block_on(future);
    runtime.shutdown_timeout(RUNTIME_SHUTDOWN_LIMIT);
    Ok(output)
}

fn main() -> ExitCode {
    tracing_subscriber::fmt()
        .with_env_filter(
            EnvFilter::try_from_env("INENGINE_LOG").unwrap_or_else(|_| EnvFilter::new("warn")),
        )
        .with_ansi(std::io::stdout().is_terminal())
        .init();

    let cli = Cli::parse();

    let loaded = match config::load(Path::new(&cli.config)) {
        Ok(loaded) => loaded,
        Err(e) => {
            eprintln!("Error: {e}");
            return ExitCode::FAILURE;
        }
    };
    if loaded.report.has_issues() {
        if std::io::stderr().is_terminal() {
            eprint!("{}", loaded.render_pretty());
        } else {
            eprint!("{}", loaded.render_plain());
        }
    }
    let Some(settings) = loaded.settings else {
        return ExitCode::FAILURE;
    };
    let settings = Arc::new(settings);

    if cli.server {
        return run_server(settings);
    }

    let subcommand = match cli.subcommand {
        Some(sc) => sc,
        None => {
            let _ = Cli::command().print_help();
            println!();
            return ExitCode::SUCCESS;
        }
    };

    let mut ctx = CommandContext::new(Output::new(std::io::stdout().is_terminal()), settings);

    let result = match subcommand {
        CliCommand::Echo(args) => {
            let cmd = Echo { text: args.text };
            cmd.run(&mut ctx)
        }
        CliCommand::Exec(args) => {
            let cmd = Exec {
                executable: args.executable,
                arguments: args.arguments,
                timeout: args.timeout,
            };
            cmd.run(&mut ctx)
        }
        CliCommand::Queue(queue_cmd) => match run_queue_subcommand(queue_cmd, &mut ctx) {
            Ok(result) => result,
            Err(error) => {
                eprintln!("Error: {RUNTIME_START_FAILURE}: {error}");
                return ExitCode::FAILURE;
            }
        },
        CliCommand::Schedule(schedule_cmd) => match schedule_cmd {
            ScheduleSubcommand::List => {
                let plugins = load_plugins(&ctx.settings);
                let (schedule, schedule_errors) = schedule_with_plugins(plugins.as_ref());
                ctx.output
                    .text(&render_schedule(&schedule, &schedule_errors, Utc::now()));
                Ok(())
            }
        },
        CliCommand::Plugin(args) => {
            let plugins = load_plugins(&ctx.settings);
            run_plugin_command(
                plugins.as_ref(),
                &args.name,
                &args.command,
                &args.args,
                &mut ctx,
            )
        }
    };

    ctx.output.print();

    match result {
        Ok(()) => ExitCode::SUCCESS,
        Err(error) => {
            eprint!("{}", describe_error_chain(&error));
            ExitCode::FAILURE
        }
    }
}

fn run_server(settings: Arc<config::Settings>) -> ExitCode {
    let plugins = load_plugins(&settings);
    let registry = Arc::new(registry_with_plugins(plugins.as_ref()));
    let (schedule, schedule_errors) = schedule_with_plugins(plugins.as_ref());
    log_plugin_errors(&schedule_errors);
    let served = run_async(async move {
        let cancel = tokio_util::sync::CancellationToken::new();

        let cancel_ctrlc = cancel.clone();
        tokio::spawn(async move {
            tokio::signal::ctrl_c().await.ok();
            cancel_ctrlc.cancel();
        });

        #[cfg(unix)]
        {
            let cancel_term = cancel.clone();
            tokio::spawn(async move {
                let mut sig =
                    tokio::signal::unix::signal(tokio::signal::unix::SignalKind::terminate())
                        .expect("failed to register SIGTERM handler");
                sig.recv().await;
                cancel_term.cancel();
            });
        }

        Server::new(settings, registry, schedule).run(cancel).await;
    });
    match served {
        Ok(()) => ExitCode::SUCCESS,
        Err(error) => {
            eprintln!("Error: {RUNTIME_START_FAILURE}: {error}");
            ExitCode::FAILURE
        }
    }
}

/// The outer `Err` means the runtime did not start, and the request did not run.
fn run_queue_subcommand(
    queue_cmd: QueueSubcommand,
    ctx: &mut CommandContext,
) -> std::io::Result<CommandResult> {
    let queue_settings = &ctx.settings.queue;
    let output = &mut ctx.output;
    match queue_cmd {
        QueueSubcommand::Publish(args) => {
            let request = Publish {
                plugin_name: args.plugin,
                command_name: args.command,
                args: args.args,
                json: args.json,
                max_retries: args.max_retries,
                lane: lane_from_secondary_flag(args.secondary),
            };
            run_async(request.run(queue_settings, output))
        }
        QueueSubcommand::Consume(args) => {
            let plugins = load_plugins(&ctx.settings);
            let request = Consume {
                limit: (!args.all).then_some(args.count),
                lane: lane_from_secondary_flag(args.secondary),
                registry: Arc::new(registry_with_plugins(plugins.as_ref())),
            };
            run_async(request.run(ctx.settings.clone(), output))
        }
        QueueSubcommand::Flush(args) => {
            let request = Clear {
                pending: args.pending,
                failed: args.failed,
                in_progress: args.in_progress,
                lane: lane_from_secondary_flag(args.secondary),
            };
            run_async(request.run(queue_settings, output))
        }
        QueueSubcommand::Length | QueueSubcommand::Status => {
            run_async(write_lengths(queue_settings, output))
        }
        QueueSubcommand::Peek(args) => {
            let request = Peek {
                range: args.from..=args.to,
                json_format: args.json,
                pending: args.pending,
                failed: args.failed,
                in_progress: args.in_progress,
                lane: lane_from_secondary_flag(args.secondary),
            };
            run_async(request.run(queue_settings, output))
        }
        QueueSubcommand::Republish(args) => {
            let request = RepublishFailed {
                limit: args.limit,
                lane: lane_from_secondary_flag(args.secondary),
            };
            run_async(request.run(queue_settings, output))
        }
        QueueSubcommand::Recover(args) => {
            let request = Recover {
                lane: lane_from_secondary_flag(args.secondary),
            };
            run_async(request.run(queue_settings, output))
        }
    }
}

fn lane_from_secondary_flag(secondary: bool) -> QueueLane {
    if secondary {
        QueueLane::Secondary
    } else {
        QueueLane::Primary
    }
}
