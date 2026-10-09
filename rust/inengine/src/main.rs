mod cli;

use std::io::IsTerminal;
use std::path::Path;
use std::process::ExitCode;
use std::sync::Arc;

use chrono::{DateTime, Utc};
use clap::{CommandFactory, Parser};
use tracing::warn;
use tracing_subscriber::EnvFilter;

use inengine_core::command::{Command, CommandContext, CommandResult};
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

use inengine_core::config::Settings;
use inengine_core::error::PluginError;
use inengine_core::plugin::PluginLoader;
use inengine_core::scheduling::schedule::Schedule;

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
        let plugins = load_plugins(&settings);
        let registry = Arc::new(registry_with_plugins(plugins.as_ref()));
        let (schedule, schedule_errors) = schedule_with_plugins(plugins.as_ref());
        log_plugin_errors(&schedule_errors);
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

        let server = Server::new(settings, registry, schedule);
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
        CliCommand::Queue(queue_cmd) => match queue_cmd {
            QueueSubcommand::Publish(args) => {
                let cmd = Publish {
                    plugin_name: args.plugin,
                    command_name: args.command,
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
                    registry: Arc::new(registry_with_plugins(load_plugins(&ctx.settings).as_ref())),
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

/// Loads the plugins that the settings list and logs each failure. A runtime that fails to
/// start leaves every plugin unloaded, and the built-in commands still run.
fn load_plugins(settings: &Settings) -> Option<PluginLoader> {
    let (loader, errors) = PluginLoader::from_settings(settings);
    log_plugin_errors(&errors);
    loader
}

fn run_plugin_command(
    plugins: Option<&PluginLoader>,
    plugin_name: &str,
    command_name: &str,
    args: &[String],
    ctx: &mut CommandContext,
) -> CommandResult {
    let loaded = plugins.is_some_and(|loader| {
        loader
            .plugins()
            .iter()
            .any(|plugin| plugin.name == plugin_name)
    });
    if !loaded {
        return Err(CommandError::failed(format!(
            "plugin '{plugin_name}' is not loaded"
        )));
    }
    registry_with_plugins(plugins)
        .build(plugin_name, command_name, &args_to_json(args))
        .map_err(|error| CommandError::failed_with("could not run the plugin command", error))?
        .run(ctx)
}

fn log_plugin_errors(errors: &[PluginError]) {
    for error in errors {
        warn!("{error}");
    }
}

fn registry_with_plugins(plugins: Option<&PluginLoader>) -> CommandRegistry {
    let mut registry = CommandRegistry::new();
    if let Some(loader) = plugins {
        log_plugin_errors(&loader.register_commands(&mut registry));
    }
    registry
}

fn schedule_with_plugins(plugins: Option<&PluginLoader>) -> (Schedule, Vec<PluginError>) {
    let mut schedule = Schedule::new();
    let errors = plugins
        .map(|loader| loader.register_schedules(&mut schedule))
        .unwrap_or_default();
    (schedule, errors)
}

fn args_to_json(args: &[String]) -> String {
    let fields: serde_json::Map<String, serde_json::Value> = args
        .iter()
        .filter_map(|arg| arg.split_once('='))
        .map(|(key, value)| {
            (
                key.to_string(),
                serde_json::Value::String(value.to_string()),
            )
        })
        .collect();
    serde_json::Value::Object(fields).to_string()
}

fn render_schedule(schedule: &Schedule, errors: &[PluginError], now: DateTime<Utc>) -> String {
    let mut table = String::new();
    if schedule.jobs().is_empty() {
        table.push_str("No scheduled commands.\n");
    } else {
        table.push_str(&format!(
            "{:<30}{:<25}{}\n",
            "Job", "Cron Expression", "Next Fire Time"
        ));
        for job in schedule.jobs() {
            let next = job
                .cron_schedule
                .after(&now)
                .next()
                .map(|time| time.to_string())
                .unwrap_or_else(|| "none".to_string());
            table.push_str(&format!(
                "{:<30}{:<25}{next}\n",
                job.name, job.cron_expression
            ));
        }
    }
    for error in errors {
        table.push_str(&format!("warning: {error}\n"));
    }
    table
}

#[cfg(test)]
mod tests {
    use std::collections::HashMap;

    use chrono::TimeZone;
    use inengine_core::config::PluginLimitsSettings;
    use inengine_core::plugin::PluginRuntime;

    use super::*;

    fn fixture_plugins() -> PluginLoader {
        let fixture =
            Path::new(env!("CARGO_MANIFEST_DIR")).join("../inengine-core/tests/fixtures/plugin");
        let directories = HashMap::from([(
            "fixture".to_string(),
            fixture.to_string_lossy().into_owned(),
        )]);
        let runtime = PluginRuntime::new(PluginLimitsSettings::default()).unwrap();
        let (loader, errors) = PluginLoader::load(&directories, runtime);
        assert!(errors.is_empty(), "{errors:?}");
        loader
    }

    fn context() -> CommandContext {
        let settings = config::from_spec(&config::SettingsSpec::default())
            .settings
            .expect("the default settings are valid");
        CommandContext::new(Output::new(false), Arc::new(settings))
    }

    struct PluginJobCommand;

    impl Command for PluginJobCommand {
        fn name(&self) -> &str {
            "fixture::echo-args"
        }

        fn run(&self, _ctx: &mut CommandContext) -> CommandResult {
            Ok(())
        }
    }

    #[test]
    fn schedule_table_lists_each_job_and_each_warning() {
        // Arrange
        let mut schedule = Schedule::new();
        let job = schedule
            .command(PluginJobCommand)
            .cron("0 0 0 1 1 *")
            .unwrap();
        schedule.add(job);
        let errors = [PluginError::InvalidScheduleEntry {
            plugin: "fixture".to_string(),
            command: "missing".to_string(),
            reason: "the plugin does not list this command".to_string(),
        }];
        let now = Utc.with_ymd_and_hms(2026, 6, 1, 0, 0, 0).unwrap();

        // Act
        let table = render_schedule(&schedule, &errors, now);

        // Assert
        let lines: Vec<&str> = table.lines().collect();
        assert_eq!(lines.len(), 3);
        assert!(lines[0].starts_with("Job"));
        assert!(lines[1].starts_with("fixture::echo-args"));
        assert!(lines[1].contains("0 0 0 1 1 *"));
        assert!(lines[1].ends_with("2027-01-01 00:00:00 UTC"));
        assert_eq!(
            lines[2],
            "warning: invalid schedule entry for command 'missing' in plugin 'fixture': the plugin does not list this command"
        );
    }

    #[test]
    fn empty_schedule_says_so() {
        // Arrange
        let schedule = Schedule::new();

        // Act
        let table = render_schedule(&schedule, &[], Utc::now());

        // Assert
        assert_eq!(table, "No scheduled commands.\n");
    }

    #[test]
    fn plugin_args_become_one_json_object() {
        // Arrange
        let args = ["name=Ethan".to_string(), "count=2".to_string()];

        // Act
        let json = args_to_json(&args);

        // Assert
        let value: serde_json::Value = serde_json::from_str(&json).unwrap();
        assert_eq!(value, serde_json::json!({"name": "Ethan", "count": "2"}));
    }

    #[test]
    fn plugin_command_runs_with_its_arguments() {
        // Arrange
        let plugins = fixture_plugins();
        let mut ctx = context();
        let args = ["name=Ethan".to_string()];

        // Act
        let result = run_plugin_command(Some(&plugins), "fixture", "echo-args", &args, &mut ctx);

        // Assert
        assert!(result.is_ok(), "{result:?}");
        assert_eq!(ctx.output.take_buffer(), "{\"name\":\"Ethan\"}\n");
    }

    #[test]
    fn unknown_plugin_command_fails_before_the_plugin_runs() {
        // Arrange
        let plugins = fixture_plugins();
        let mut ctx = context();

        // Act
        let result = run_plugin_command(Some(&plugins), "fixture", "nope", &[], &mut ctx);

        // Assert
        let error = result.expect_err("the fixture does not list nope");
        let cause = std::error::Error::source(&error)
            .expect("a cause")
            .to_string();
        assert_eq!(cause, "command not found in plugin 'fixture': nope");
    }

    #[test]
    fn plugin_that_is_not_loaded_is_reported_as_such() {
        // Arrange
        let plugins = fixture_plugins();
        let mut ctx = context();

        // Act
        let result = run_plugin_command(Some(&plugins), "missing", "run", &[], &mut ctx);

        // Assert
        let error = result.expect_err("no plugin is named missing");
        assert_eq!(
            error.to_string(),
            "command failed: plugin 'missing' is not loaded"
        );
    }

    #[test]
    fn schedule_table_from_the_fixture_lists_one_job_and_two_warnings() {
        // Arrange
        let plugins = fixture_plugins();
        let (schedule, errors) = schedule_with_plugins(Some(&plugins));
        let now = Utc.with_ymd_and_hms(2026, 6, 1, 0, 0, 0).unwrap();

        // Act
        let table = render_schedule(&schedule, &errors, now);

        // Assert
        let lines: Vec<&str> = table.lines().collect();
        assert_eq!(lines.len(), 4, "{table}");
        assert!(lines[1].starts_with("fixture::echo-args"));
        assert!(lines[2].starts_with("warning: invalid schedule entry for command 'missing'"));
        assert!(lines[3].starts_with("warning: invalid schedule entry for command 'echo-args'"));
    }
}
