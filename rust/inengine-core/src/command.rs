use std::sync::Arc;
use std::time::{Duration, Instant};

use serde::de::DeserializeOwned;
use serde::Serialize;
use tokio_util::sync::CancellationToken;

use crate::config::Settings;
use crate::error::CommandError;
use crate::output::Output;

pub type CommandResult = Result<(), CommandError>;

/// A unit of work. `run` may block, so a caller runs it on a thread that may block: the main
/// thread of a synchronous program, a plain thread, or a `spawn_blocking` thread. It never
/// runs on an async worker thread, and it does not reach the tokio runtime.
pub trait Command: Send + Sync {
    fn name(&self) -> &str;
    fn run(&self, ctx: &mut CommandContext) -> CommandResult;
}

/// A command panicked on its blocking thread.
#[derive(Debug, thiserror::Error)]
#[error(transparent)]
pub(crate) struct CommandPanicked(#[from] tokio::task::JoinError);

impl From<CommandPanicked> for CommandError {
    fn from(panicked: CommandPanicked) -> Self {
        CommandError::failed_with("the command panicked", panicked)
    }
}

/// Runs `command` on the blocking thread pool and returns its result with the text it wrote.
pub(crate) async fn run_blocking(
    command: Arc<dyn Command>,
    mut ctx: CommandContext,
) -> Result<(CommandResult, String), CommandPanicked> {
    let task = tokio::task::spawn_blocking(move || {
        let result = command.run(&mut ctx);
        (result, ctx.output.take_buffer())
    });
    Ok(task.await?)
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
    /// When the command must finish. A plugin call or an exec process stops at this time.
    pub deadline: Option<CommandDeadline>,
    /// Fires when the server stops. A plugin call or an exec process stops when it fires.
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

    struct PanicCommand;

    impl Command for PanicCommand {
        fn name(&self) -> &str {
            "panic"
        }

        fn run(&self, _ctx: &mut CommandContext) -> CommandResult {
            panic!("intentional panic");
        }
    }

    #[tokio::test]
    async fn run_blocking_returns_the_result_and_the_output() {
        // Arrange
        let ctx = crate::commands::test_helpers::make_ctx();

        // Act
        let returned = run_blocking(Arc::new(TestCommand), ctx).await;

        // Assert
        let (result, output) = returned.expect("the command does not panic");
        assert!(result.is_ok());
        assert_eq!(output, "test ran\n");
    }

    #[tokio::test]
    async fn run_blocking_reports_a_panic_apart_from_a_command_error() {
        // Arrange
        let ctx = crate::commands::test_helpers::make_ctx();

        // Act
        let returned = run_blocking(Arc::new(PanicCommand), ctx).await;

        // Assert
        let panicked = returned.expect_err("the command panics");
        let message = panicked.to_string();
        assert!(message.starts_with("task "), "{message}");
        assert!(message.contains("intentional panic"), "{message}");
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
