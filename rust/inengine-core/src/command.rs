use std::sync::Arc;
use std::time::{Duration, Instant};

use serde::de::DeserializeOwned;
use serde::Serialize;
use tokio_util::sync::CancellationToken;

use crate::config::Settings;
use crate::error::CommandError;
use crate::output::Output;

pub type CommandResult = Result<(), CommandError>;

pub trait Command: Send + Sync {
    fn name(&self) -> &str;
    fn run(&self, ctx: &mut CommandContext) -> CommandResult;
}

/// A command that can be queued. The two names identify it in a queued message, so changing
/// either one stops messages that are already queued from running.
pub trait QueueableCommand: Command + Serialize + DeserializeOwned + 'static {
    /// The plugin name in a queued message, which is
    /// [`BUILTIN_PLUGIN_NAME`](crate::BUILTIN_PLUGIN_NAME) for a built-in command.
    const PLUGIN_NAME: &'static str;
    /// The command name in a queued message.
    const COMMAND_NAME: &'static str;
}

/// The state a command runs with.
pub struct CommandContext {
    pub output: Output,
    pub settings: Arc<Settings>,
    /// When the command must finish. A plugin call stops at this time.
    pub deadline: Option<CommandDeadline>,
    /// Fires when the server stops. A plugin call stops when it fires.
    pub cancel: CancellationToken,
}

/// A time limit for one command run.
#[derive(Debug, Clone, Copy)]
pub struct CommandDeadline {
    pub at: Instant,
    /// The limit the deadline came from, reported when the command times out.
    pub timeout: Duration,
}

impl CommandDeadline {
    /// A deadline `timeout` from now.
    pub fn after(timeout: Duration) -> Self {
        Self {
            at: instant_after(timeout),
            timeout,
        }
    }
}

/// `Instant + Duration` panics on overflow, so a limit too large to represent becomes a time
/// that no command reaches.
pub(crate) fn instant_after(duration: Duration) -> Instant {
    const FAR_FUTURE: Duration = Duration::from_secs(100 * 365 * 24 * 60 * 60);
    let now = Instant::now();
    now.checked_add(duration)
        .or_else(|| now.checked_add(FAR_FUTURE))
        .unwrap_or(now)
}

impl CommandContext {
    /// A context with no deadline and a cancellation token that never fires.
    pub fn new(output: Output, settings: Arc<Settings>) -> Self {
        Self {
            output,
            settings,
            deadline: None,
            cancel: CancellationToken::new(),
        }
    }

    /// Gives the command a time limit.
    pub fn with_deadline(mut self, deadline: CommandDeadline) -> Self {
        self.deadline = Some(deadline);
        self
    }

    /// Gives the command the token that fires when the server stops.
    pub fn with_cancel(mut self, cancel: CancellationToken) -> Self {
        self.cancel = cancel;
        self
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    struct TestCommand;

    impl Command for TestCommand {
        fn name(&self) -> &str {
            "test"
        }

        fn run(&self, ctx: &mut CommandContext) -> CommandResult {
            ctx.output.info_line("test ran");
            Ok(())
        }
    }

    #[test]
    fn command_can_be_called_via_run() {
        // Arrange
        let settings = crate::config::from_spec(&crate::config::SettingsSpec::default())
            .settings
            .expect("the default settings are valid");
        let settings = Arc::new(settings);
        let mut ctx = CommandContext::new(Output::new(false), settings);
        let cmd = TestCommand;

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok());
        assert_eq!(cmd.name(), "test");
        let buf = ctx.output.take_buffer();
        assert!(buf.contains("test ran"));
    }

    #[test]
    fn command_error_failed_carries_message() {
        // Arrange
        let error = CommandError::failed("something broke");

        // Act
        let display = format!("{error}");

        // Assert
        assert_eq!(display, "command failed: something broke");
    }

    #[test]
    fn command_error_timeout_carries_seconds() {
        // Arrange
        let error = CommandError::Timeout { seconds: 30 };

        // Act
        let display = format!("{error}");

        // Assert
        assert_eq!(display, "command timed out after 30 seconds");
    }
}
