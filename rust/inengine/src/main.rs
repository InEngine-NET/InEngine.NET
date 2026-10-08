mod cli;

use std::io::IsTerminal;
use std::path::Path;
use std::process::ExitCode;
use std::sync::Arc;

use clap::{CommandFactory, Parser};
use tracing_subscriber::EnvFilter;

use inengine_core::command::{Command, CommandContext};
use inengine_core::commands::echo::Echo;
use inengine_core::commands::exec::Exec;
use inengine_core::config;
use inengine_core::error::CommandError;
use inengine_core::output::Output;
use inengine_core::queue::registry::CommandRegistry;
use inengine_core::queue::QueueLane;
use inengine_core::queue_commands::management::{
    Clear, Length, Peek, Recover, RepublishFailed, Status,
};
use inengine_core::queue_commands::messaging::{Consume, Publish};
use inengine_core::server::host::Server;

use inengine_core::plugin::loader::PluginLoader;
use inengine_core::plugin::wasm_command::WasmCommand;

use cli::{Cli, CliCommand, QueueSubcommand, ScheduleSubcommand};

#[tokio::main]
async fn main() -> ExitCode {
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
        let mut registry = CommandRegistry::new();
        let mut plugin_loader = PluginLoader::new().unwrap_or_else(|e| {
            eprintln!("Error initializing plugin loader: {e}");
            std::process::exit(1);
        });
        if let Err(e) = plugin_loader.load_from_directories(&settings.plugins) {
            eprintln!("Error loading plugins: {e}");
        }
        for plugin in &plugin_loader.plugins {
            for desc in &plugin.command_descriptors {
                let key = format!("{}::{}", plugin.name, desc.name);
                let component = plugin.component.clone();
                let engine = plugin_loader.engine().clone();
                let pname = plugin.name.clone();
                let cname = desc.name.clone();
                registry.register(&key, move |json| {
                    Ok(Box::new(WasmCommand {
                        plugin_name: pname.clone(),
                        command_name: cname.clone(),
                        args_json: json.to_string(),
                        component: component.clone(),
                        engine: engine.clone(),
                    }))
                });
            }
        }
        let registry = Arc::new(registry);
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

        let server = Server::new(settings, registry);
        server.run(cancel).await;
        return ExitCode::SUCCESS;
    }

    let subcommand = match cli.subcommand {
        Some(sc) => sc,
        None => {
            let _ = Cli::command().print_help();
            println!();
            return ExitCode::SUCCESS;
        }
    };

    let mut ctx = CommandContext {
        output: Output::new(std::io::stdout().is_terminal()),
        settings,
    };

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
        CliCommand::Queue(queue_cmd) => match queue_cmd {
            QueueSubcommand::Publish(args) => {
                let cmd = Publish {
                    plugin_name: args.plugin,
                    command_class: args.class,
                    command_verb: args.command,
                    args: args.args,
                    json: args.json,
                    max_retries: args.max_retries,
                    lane: lane_from_secondary_flag(args.secondary),
                };
                cmd.run(&mut ctx)
            }
            QueueSubcommand::Consume(args) => {
                let cmd = Consume {
                    limit: (!args.all).then_some(args.count),
                    lane: lane_from_secondary_flag(args.secondary),
                    registry: Arc::new(CommandRegistry::new()),
                };
                cmd.run(&mut ctx)
            }
            QueueSubcommand::Flush(args) => {
                let cmd = Clear {
                    pending: args.pending,
                    failed: args.failed,
                    in_progress: args.in_progress,
                    lane: lane_from_secondary_flag(args.secondary),
                };
                cmd.run(&mut ctx)
            }
            QueueSubcommand::Length => {
                let cmd = Length;
                cmd.run(&mut ctx)
            }
            QueueSubcommand::Peek(args) => {
                let cmd = Peek {
                    range: args.from..=args.to,
                    json_format: args.json,
                    pending: args.pending,
                    failed: args.failed,
                    in_progress: args.in_progress,
                    lane: lane_from_secondary_flag(args.secondary),
                };
                cmd.run(&mut ctx)
            }
            QueueSubcommand::Status => {
                let cmd = Status;
                cmd.run(&mut ctx)
            }
            QueueSubcommand::Republish(args) => {
                let cmd = RepublishFailed {
                    limit: args.limit,
                    lane: lane_from_secondary_flag(args.secondary),
                };
                cmd.run(&mut ctx)
            }
            QueueSubcommand::Recover(args) => {
                let cmd = Recover {
                    lane: lane_from_secondary_flag(args.secondary),
                };
                cmd.run(&mut ctx)
            }
        },
        CliCommand::Schedule(schedule_cmd) => match schedule_cmd {
            ScheduleSubcommand::List => {
                use chrono::Utc;
                use inengine_core::scheduling::scheduler::Scheduler;

                let scheduler = Scheduler::new(ctx.settings.clone());
                if scheduler.schedule.jobs.is_empty() {
                    ctx.output.line("No scheduled commands.");
                } else {
                    ctx.output
                        .info(&format!("{:<20}", "Command"))
                        .info(&format!("{:<25}", "Cron Expression"))
                        .info_line("Next Fire Time");
                    for job in &scheduler.schedule.jobs {
                        let next = job
                            .cron_schedule
                            .upcoming(Utc)
                            .next()
                            .map(|t| t.to_string())
                            .unwrap_or_else(|| "none".to_string());
                        ctx.output
                            .text(&format!("{:<20}", job.command_name))
                            .text(&format!("{:<25}", job.cron_expression))
                            .line(&next);
                    }
                }
                Ok(())
            }
        },
        CliCommand::Plugin(args) => {
            let plugin_loader = match PluginLoader::new() {
                Ok(mut loader) => {
                    if let Err(e) = loader.load_from_directories(&ctx.settings.plugins) {
                        eprintln!("Error loading plugins: {e}");
                    }
                    loader
                }
                Err(e) => {
                    eprintln!("Error initializing plugin loader: {e}");
                    ctx.output.print();
                    return ExitCode::FAILURE;
                }
            };

            let mut fields = serde_json::Map::new();
            for arg in &args.args {
                if let Some((key, value)) = arg.split_once('=') {
                    fields.insert(
                        key.to_string(),
                        serde_json::Value::String(value.to_string()),
                    );
                }
            }
            let args_json = serde_json::to_string(&fields).unwrap_or_else(|_| "{}".to_string());

            let plugin = plugin_loader.plugins.iter().find(|p| p.name == args.name);

            match plugin {
                Some(p) => {
                    let cmd = WasmCommand {
                        plugin_name: p.name.clone(),
                        command_name: args.command.clone(),
                        args_json,
                        component: p.component.clone(),
                        engine: plugin_loader.engine().clone(),
                    };
                    cmd.run(&mut ctx)
                }
                None => Err(CommandError::failed(format!(
                    "plugin '{}' not found",
                    args.name
                ))),
            }
        }
    };

    ctx.output.print();

    match result {
        Ok(()) => ExitCode::SUCCESS,
        Err(e) => {
            eprintln!("Error: {e}");
            let mut source = std::error::Error::source(&e);
            while let Some(cause) = source {
                eprintln!("  caused by: {cause}");
                source = cause.source();
            }
            ExitCode::FAILURE
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
