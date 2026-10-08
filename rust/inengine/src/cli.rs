use clap::{Args, Parser};

#[derive(Parser)]
#[command(name = "inengine", about = "A command queuing and scheduling server")]
pub struct Cli {
    #[arg(short = 's', long = "server", help = "Run InEngine server")]
    pub server: bool,

    #[arg(
        short = 'c',
        long = "configuration",
        default_value = "./inengine.toml",
        help = "Path to the configuration file"
    )]
    pub config: String,

    #[command(subcommand)]
    pub subcommand: Option<Subcommand>,
}

#[derive(Debug, clap::Subcommand)]
pub enum Subcommand {
    Echo(EchoArgs),
    Exec(ExecArgs),
    #[command(subcommand)]
    Queue(QueueSubcommand),
    #[command(subcommand)]
    Schedule(ScheduleSubcommand),
    Plugin(PluginArgs),
}

#[derive(Debug, clap::Subcommand)]
pub enum ScheduleSubcommand {
    List,
}

#[derive(Debug, clap::Subcommand)]
pub enum QueueSubcommand {
    Publish(QueuePublishArgs),
    Consume(QueueConsumeArgs),
    Flush(QueueFlushArgs),
    Length,
    Peek(QueuePeekArgs),
    Status,
    Republish(QueueRepublishArgs),
    /// Move in-progress messages back to pending.
    ///
    /// Stop every other consumer of the queue first. Recovery cannot tell a message left by a
    /// crash from one that another consumer is still processing, so that message would run twice.
    Recover(QueueRecoverArgs),
}

#[derive(Debug, Args)]
pub struct EchoArgs {
    #[arg(long = "text", default_value = "", help = "The text to echo")]
    pub text: String,
}

#[derive(Debug, Args)]
pub struct ExecArgs {
    #[arg(
        short = 'e',
        long = "executable",
        help = "The whitelisted executable name"
    )]
    pub executable: String,

    #[arg(
        short = 't',
        long = "timeout",
        default_value = "900",
        help = "Seconds before killing the process, or 0 for no limit"
    )]
    pub timeout: u64,

    #[arg(last = true, help = "Arguments for the executable, after --")]
    pub arguments: Vec<String>,
}

#[derive(Debug, Args)]
pub struct PluginArgs {
    #[arg(long, help = "Plugin name")]
    pub name: String,

    #[arg(long, help = "Command name within the plugin")]
    pub command: String,

    #[arg(long, help = "Command arguments as key=value pairs")]
    pub args: Vec<String>,
}

#[derive(Debug, Args)]
pub struct QueuePublishArgs {
    #[arg(long, help = "Plugin name")]
    pub plugin: String,

    #[arg(long, help = "Command class name")]
    pub class: Option<String>,

    #[arg(long, help = "Command verb name")]
    pub command: Option<String>,

    #[arg(long, help = "Command arguments as key=value pairs")]
    pub args: Vec<String>,

    #[arg(
        long,
        conflicts_with = "args",
        help = "The whole command payload as a JSON object"
    )]
    pub json: Option<String>,

    #[arg(
        long,
        value_parser = clap::value_parser!(u32).range(0..=100),
        help = "Retries after the first attempt, from 0 to 100, in place of queue.max_retries"
    )]
    pub max_retries: Option<u32>,

    #[arg(long, help = "Publish to the secondary queue")]
    pub secondary: bool,
}

#[derive(Debug, Args)]
pub struct QueueConsumeArgs {
    #[arg(long, help = "Consume all messages")]
    pub all: bool,

    #[arg(long, default_value = "1", help = "Number of messages to consume")]
    pub count: usize,

    #[arg(long, help = "Consume from the secondary queue")]
    pub secondary: bool,
}

#[derive(Debug, Args)]
pub struct QueueFlushArgs {
    #[arg(long, help = "Clear the pending queue")]
    pub pending: bool,

    #[arg(long, help = "Clear the failed queue")]
    pub failed: bool,

    #[arg(long = "in-progress", help = "Clear the in-progress queue")]
    pub in_progress: bool,

    #[arg(long, help = "Clear secondary queues")]
    pub secondary: bool,
}

#[derive(Debug, Args)]
pub struct QueuePeekArgs {
    #[arg(long, default_value = "0", help = "First message index")]
    pub from: usize,

    #[arg(long, default_value = "9", help = "Last message index")]
    pub to: usize,

    #[arg(long, help = "View messages as JSON")]
    pub json: bool,

    #[arg(long, help = "Peek at pending messages")]
    pub pending: bool,

    #[arg(long, help = "Peek at failed messages")]
    pub failed: bool,

    #[arg(long = "in-progress", help = "Peek at in-progress messages")]
    pub in_progress: bool,

    #[arg(long, help = "Peek at secondary queue messages")]
    pub secondary: bool,
}

#[derive(Debug, Args)]
pub struct QueueRepublishArgs {
    #[arg(long, default_value = "100", help = "Maximum messages to republish")]
    pub limit: usize,

    #[arg(long, help = "Republish from the secondary queue")]
    pub secondary: bool,
}

#[derive(Debug, Args)]
pub struct QueueRecoverArgs {
    #[arg(long, help = "Recover the secondary queue")]
    pub secondary: bool,
}

#[cfg(test)]
mod tests {
    use super::*;
    use clap::Parser;

    #[test]
    fn server_flag_is_set() {
        // Arrange / Act
        let cli = Cli::parse_from(["inengine", "--server"]);

        // Assert
        assert!(cli.server);
    }

    #[test]
    fn config_path_is_set() {
        // Arrange / Act
        let cli = Cli::parse_from(["inengine", "-c", "/custom/config.json"]);

        // Assert
        assert_eq!(cli.config, "/custom/config.json");
    }

    #[test]
    fn echo_subcommand_parses_text() {
        // Arrange / Act
        let cli = Cli::parse_from(["inengine", "echo", "--text", "hello"]);

        // Assert
        match cli.subcommand {
            Some(Subcommand::Echo(args)) => assert_eq!(args.text, "hello"),
            other => panic!("expected Echo, got: {other:?}"),
        }
    }

    #[test]
    fn echo_without_text_defaults_to_empty() {
        // Arrange / Act
        let cli = Cli::parse_from(["inengine", "echo"]);

        // Assert
        match cli.subcommand {
            Some(Subcommand::Echo(args)) => assert_eq!(args.text, ""),
            other => panic!("expected Echo, got: {other:?}"),
        }
    }

    #[test]
    fn exec_subcommand_parses_args() {
        // Arrange / Act
        let cli = Cli::parse_from(["inengine", "exec", "-e", "foo", "--", "flag1", "two words"]);

        // Assert
        match cli.subcommand {
            Some(Subcommand::Exec(args)) => {
                assert_eq!(args.executable, "foo");
                assert_eq!(args.arguments, vec!["flag1", "two words"]);
                assert_eq!(args.timeout, 900);
            }
            other => panic!("expected Exec, got: {other:?}"),
        }
    }

    #[test]
    fn no_args_produces_none_subcommand() {
        // Arrange / Act
        let cli = Cli::parse_from(["inengine"]);

        // Assert
        assert!(cli.subcommand.is_none());
        assert!(!cli.server);
    }

    #[test]
    fn queue_publish_parses() {
        // Arrange / Act
        let cli = Cli::parse_from([
            "inengine",
            "queue",
            "publish",
            "--plugin",
            "inengine-core",
            "--class",
            "inengine_core::commands::echo::Echo",
            "--args",
            "text=hello",
            "--secondary",
        ]);

        // Assert
        match cli.subcommand {
            Some(Subcommand::Queue(QueueSubcommand::Publish(args))) => {
                assert_eq!(args.plugin, "inengine-core");
                assert_eq!(
                    args.class.as_deref(),
                    Some("inengine_core::commands::echo::Echo")
                );
                assert_eq!(args.args, vec!["text=hello"]);
                assert!(args.secondary);
            }
            other => panic!("expected Queue Publish, got: {other:?}"),
        }
    }

    #[test]
    fn queue_consume_all_parses() {
        // Arrange / Act
        let cli = Cli::parse_from(["inengine", "queue", "consume", "--all"]);

        // Assert
        match cli.subcommand {
            Some(Subcommand::Queue(QueueSubcommand::Consume(args))) => {
                assert!(args.all);
            }
            other => panic!("expected Queue Consume, got: {other:?}"),
        }
    }

    #[test]
    fn queue_flush_parses() {
        // Arrange / Act
        let cli = Cli::parse_from(["inengine", "queue", "flush", "--pending", "--failed"]);

        // Assert
        match cli.subcommand {
            Some(Subcommand::Queue(QueueSubcommand::Flush(args))) => {
                assert!(args.pending);
                assert!(args.failed);
                assert!(!args.in_progress);
            }
            other => panic!("expected Queue Flush, got: {other:?}"),
        }
    }

    #[test]
    fn schedule_list_parses() {
        // Arrange / Act
        let cli = Cli::parse_from(["inengine", "schedule", "list"]);

        // Assert
        assert!(matches!(
            cli.subcommand,
            Some(Subcommand::Schedule(ScheduleSubcommand::List))
        ));
    }

    #[test]
    fn queue_peek_parses() {
        // Arrange / Act
        let cli = Cli::parse_from([
            "inengine",
            "queue",
            "peek",
            "--pending",
            "--from",
            "0",
            "--to",
            "5",
        ]);

        // Assert
        match cli.subcommand {
            Some(Subcommand::Queue(QueueSubcommand::Peek(args))) => {
                assert!(args.pending);
                assert_eq!(args.from, 0);
                assert_eq!(args.to, 5);
            }
            other => panic!("expected Queue Peek, got: {other:?}"),
        }
    }

    #[test]
    fn plugin_subcommand_parses() {
        // Arrange / Act
        let cli = Cli::parse_from([
            "inengine",
            "plugin",
            "--name",
            "sample-plugin",
            "--command",
            "greet",
            "--args",
            "name=Ethan",
        ]);

        // Assert
        match cli.subcommand {
            Some(Subcommand::Plugin(args)) => {
                assert_eq!(args.name, "sample-plugin");
                assert_eq!(args.command, "greet");
                assert_eq!(args.args, vec!["name=Ethan"]);
            }
            other => panic!("expected Plugin, got: {other:?}"),
        }
    }

    #[test]
    fn queue_recover_parses_the_lane() {
        // Arrange
        let argv = ["inengine", "queue", "recover", "--secondary"];

        // Act
        let cli = Cli::parse_from(argv);

        // Assert
        match cli.subcommand {
            Some(Subcommand::Queue(QueueSubcommand::Recover(args))) => assert!(args.secondary),
            other => panic!("expected Recover, got: {other:?}"),
        }
    }

    #[test]
    fn queue_publish_parses_json_and_max_retries() {
        // Arrange
        let argv = [
            "inengine",
            "queue",
            "publish",
            "--plugin",
            "p",
            "--class",
            "c",
            "--json",
            "{}",
            "--max-retries",
            "100",
        ];

        // Act
        let cli = Cli::parse_from(argv);

        // Assert
        match cli.subcommand {
            Some(Subcommand::Queue(QueueSubcommand::Publish(args))) => {
                assert_eq!(args.json.as_deref(), Some("{}"));
                assert_eq!(args.max_retries, Some(100));
                assert!(args.args.is_empty());
            }
            other => panic!("expected Publish, got: {other:?}"),
        }
    }

    #[test]
    fn queue_publish_rejects_max_retries_above_one_hundred() {
        // Arrange
        let argv = [
            "inengine",
            "queue",
            "publish",
            "--plugin",
            "p",
            "--class",
            "c",
            "--max-retries",
            "101",
        ];

        // Act
        let result = Cli::try_parse_from(argv);

        // Assert
        let error = result.err().expect("101 is out of range");
        assert_eq!(error.kind(), clap::error::ErrorKind::ValueValidation);
    }

    #[test]
    fn queue_publish_rejects_json_with_args() {
        // Arrange
        let argv = [
            "inengine", "queue", "publish", "--plugin", "p", "--class", "c", "--json", "{}",
            "--args", "a=b",
        ];

        // Act
        let result = Cli::try_parse_from(argv);

        // Assert
        let error = result.err().expect("--json conflicts with --args");
        assert_eq!(error.kind(), clap::error::ErrorKind::ArgumentConflict);
    }
}
