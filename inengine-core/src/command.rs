use std::sync::Arc;
use std::time::{Duration, Instant};

use serde::Serialize;
use serde::de::DeserializeOwned;
use tokio_util::sync::CancellationToken;
use tracing::warn;

use crate::config::Settings;
use crate::error::CommandError;
use crate::output::Output;

/// `Ok` when a command succeeded, or the [`CommandError`] that says why it did not.
pub type CommandResult = Result<(), CommandError>;

/// A unit of work. `run` may block, so a caller runs it on a thread that may block: the main
/// thread of a synchronous program, a plain thread, or a `spawn_blocking` thread. It never
/// runs on an async worker thread, and it does not reach the tokio runtime.
pub trait Command: Send + Sync {
    /// The command name that logs and status messages show.
    fn name(&self) -> &str;
    /// Does the work and writes its text to `ctx.output`. If the work can take long, stop it
    /// when `ctx.deadline` passes or `ctx.cancel` fires.
    fn run(&self, ctx: &mut CommandContext) -> CommandResult;
}

/// How long past its deadline `run_blocking` waits before it gives up on a command. A command
/// that honors its deadline returns its own timeout within this time, with its output.
pub(crate) const ENFORCEMENT_GRACE: Duration = Duration::from_secs(1);

/// Why `run_blocking` has no result from a command.
#[derive(Debug, thiserror::Error)]
pub(crate) enum RunBlockingError {
    /// The command panicked, or the runtime shut down before the blocking task started. The
    /// conversion to `CommandError` reports a shutdown as `Interrupted`.
    #[error(transparent)]
    Panicked(#[from] tokio::task::JoinError),
    /// The command ran past its deadline and `ENFORCEMENT_GRACE`.
    #[error("the command did not stop at its deadline")]
    DeadlineIgnored { seconds: u64 },
}

impl From<RunBlockingError> for CommandError {
    fn from(error: RunBlockingError) -> Self {
        match error {
            RunBlockingError::Panicked(ref join_error) if join_error.is_cancelled() => {
                CommandError::Interrupted
            }
            RunBlockingError::Panicked(_) => {
                CommandError::failed_with("the command panicked", error)
            }
            RunBlockingError::DeadlineIgnored { seconds } => CommandError::Timeout { seconds },
        }
    }
}

/// Runs `command` on the blocking thread pool and returns its result with the text it wrote.
///
/// When the context has a deadline, this waits until the deadline plus `ENFORCEMENT_GRACE`.
/// A command still running then keeps its thread until it returns, and its result is lost.
pub(crate) async fn run_blocking(
    command: Arc<dyn Command>,
    mut ctx: CommandContext,
) -> Result<(CommandResult, String), RunBlockingError> {
    let deadline = ctx.deadline;
    let running = Arc::clone(&command);
    let task = tokio::task::spawn_blocking(move || {
        let result = command.run(&mut ctx);
        (result, ctx.output.take_buffer())
    });
    let backstop = deadline.and_then(|deadline| {
        let at = deadline.at.checked_add(ENFORCEMENT_GRACE)?;
        Some((
            tokio::time::Instant::from_std(at),
            deadline.timeout.as_secs(),
        ))
    });
    let Some((at, seconds)) = backstop else {
        return Ok(task.await?);
    };
    match tokio::time::timeout_at(at, task).await {
        Ok(joined) => Ok(joined?),
        Err(_elapsed) => {
            warn!(
                "{} did not stop at its deadline, so the server stopped waiting for it",
                running.name()
            );
            Err(RunBlockingError::DeadlineIgnored { seconds })
        }
    }
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
    /// The buffer that the command writes its text to. The caller reads it after the run.
    pub output: Output,
    /// The settings from the configuration file.
    pub settings: Arc<Settings>,
    /// When the command must stop. A plugin call or an exec process stops at this time.
    pub deadline: Option<CommandDeadline>,
    /// Fires when the server stops. A plugin call or an exec process stops when it fires.
    pub cancel: CancellationToken,
}

/// A time limit for one command run.
#[derive(Debug, Clone, Copy)]
pub struct CommandDeadline {
    /// The time at which the command must stop.
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

    struct SleepCommand(Duration);

    impl Command for SleepCommand {
        fn name(&self) -> &str {
            "sleep"
        }

        fn run(&self, _ctx: &mut CommandContext) -> CommandResult {
            std::thread::sleep(self.0);
            Ok(())
        }
    }

    #[tokio::test]
    async fn run_blocking_returns_deadline_ignored_after_the_grace() {
        // Arrange
        let ctx = crate::commands::test_helpers::make_ctx()
            .with_deadline(CommandDeadline::after(Duration::ZERO));
        let started = Instant::now();

        // Act
        let returned = run_blocking(Arc::new(SleepCommand(Duration::from_secs(5))), ctx).await;

        // Assert
        let elapsed = started.elapsed();
        assert!(
            matches!(
                returned,
                Err(RunBlockingError::DeadlineIgnored { seconds: 0 })
            ),
            "{returned:?}"
        );
        assert!(elapsed >= ENFORCEMENT_GRACE, "{elapsed:?}");
        assert!(elapsed < Duration::from_secs(3), "{elapsed:?}");
        assert!(matches!(
            CommandError::from(returned.unwrap_err()),
            CommandError::Timeout { seconds: 0 }
        ));
    }

    #[tokio::test]
    async fn run_blocking_without_a_deadline_waits_for_the_command() {
        // Arrange
        let ctx = crate::commands::test_helpers::make_ctx();
        let started = Instant::now();

        // Act
        let returned = run_blocking(
            Arc::new(SleepCommand(ENFORCEMENT_GRACE + Duration::from_millis(500))),
            ctx,
        )
        .await;

        // Assert
        let (result, _output) = returned.expect("the command finishes");
        assert!(result.is_ok());
        assert!(started.elapsed() >= ENFORCEMENT_GRACE + Duration::from_millis(500));
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
