use std::sync::Arc;
use std::time::Duration;

use tokio::io::AsyncWriteExt;
use tokio_util::sync::CancellationToken;
use tracing::warn;

use crate::command::{run_blocking, Command, CommandContext, CommandDeadline, CommandResult};
use crate::config::Settings;
use crate::error::{describe_error_chain, CommandError};
use crate::output::Output;
use crate::server::notify::{ping, send_email};

/// How long past the deadline the lifecycle waits before it gives up on a command. A command
/// that honors its deadline returns its own timeout within this time, with its output.
const ENFORCEMENT_GRACE: Duration = Duration::from_secs(1);

/// The hooks run on an async worker thread, so they must not block.
#[derive(Default)]
pub struct CommandLifecycle {
    pub before_run: Option<Box<dyn Fn() + Send + Sync>>,
    pub after_run: Option<Box<dyn Fn() + Send + Sync>>,
    pub ping_before_url: Option<String>,
    pub ping_after_url: Option<String>,
    pub write_output_to: Option<String>,
    pub append_output_to: Option<String>,
    pub email_output_to: Option<String>,
    pub timeout: Option<Duration>,
}

/// `cancel` reaches the command through its context, so a plugin call or a process stops when
/// it fires.
///
/// The output is written, appended, and mailed when the command succeeds or fails. A command
/// that the server stopped, that ignored its timeout, or that panicked persists nothing.
pub async fn run_with_lifecycle(
    cmd: Arc<dyn Command>,
    settings: Arc<Settings>,
    lifecycle: &CommandLifecycle,
    cancel: &CancellationToken,
) -> CommandResult {
    if let Some(ref url) = lifecycle.ping_before_url {
        ping(url).await?;
    }

    if let Some(ref action) = lifecycle.before_run {
        action();
    }

    let ctx = CommandContext::new(Output::new(false), settings.clone()).with_cancel(cancel.clone());
    let returned = match lifecycle.timeout {
        Some(timeout) => {
            let ctx = ctx.with_deadline(CommandDeadline::after(timeout));
            tokio::time::timeout(
                timeout.saturating_add(ENFORCEMENT_GRACE),
                run_blocking(cmd.clone(), ctx),
            )
            .await
            .map_err(|_elapsed| CommandError::Timeout {
                seconds: timeout.as_secs(),
            })?
        }
        None => run_blocking(cmd.clone(), ctx).await,
    };
    let (cmd_result, output_text) = returned?;

    match cmd_result {
        Ok(()) => {
            if let Some(ref action) = lifecycle.after_run {
                action();
            }
            if let Some(ref url) = lifecycle.ping_after_url {
                ping(url).await?;
            }
            let subject = format!("Command completed: {}", cmd.name());
            persist_output(lifecycle, &settings, &output_text, &subject, &output_text).await
        }
        Err(CommandError::Interrupted) => Err(CommandError::Interrupted),
        Err(error) => {
            let subject = format!("Command failed: {}", cmd.name());
            let mut mail_body = output_text.clone();
            if !mail_body.is_empty() && !mail_body.ends_with('\n') {
                mail_body.push('\n');
            }
            mail_body.push_str(&describe_error_chain(&error));
            if let Err(persist_error) =
                persist_output(lifecycle, &settings, &output_text, &subject, &mail_body).await
            {
                warn!(
                    "could not save the output of {}: {persist_error}",
                    cmd.name()
                );
            }
            Err(error)
        }
    }
}

/// Writes and appends `text`, and mails `mail_body`, as the lifecycle asks.
async fn persist_output(
    lifecycle: &CommandLifecycle,
    settings: &Settings,
    text: &str,
    mail_subject: &str,
    mail_body: &str,
) -> CommandResult {
    if let Some(ref path) = lifecycle.write_output_to {
        tokio::fs::write(path, text).await.map_err(|e| {
            CommandError::failed_with(format!("failed to write output to {path}"), e)
        })?;
    }

    if let Some(ref path) = lifecycle.append_output_to {
        let mut file = tokio::fs::OpenOptions::new()
            .create(true)
            .append(true)
            .open(path)
            .await
            .map_err(|e| {
                CommandError::failed_with(format!("failed to open {path} for append"), e)
            })?;
        let appended = match file.write_all(text.as_bytes()).await {
            Ok(()) => file.flush().await,
            Err(error) => Err(error),
        };
        appended.map_err(|e| {
            CommandError::failed_with(format!("failed to append output to {path}"), e)
        })?;
    }

    if let Some(ref address) = lifecycle.email_output_to {
        send_email(&settings.mail, address, mail_subject, mail_body).await?;
    }

    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::commands::test_helpers::make_ctx;
    use std::sync::atomic::{AtomicUsize, Ordering};

    struct OkCommand;
    impl Command for OkCommand {
        fn name(&self) -> &str {
            "ok"
        }
        fn run(&self, ctx: &mut CommandContext) -> CommandResult {
            ctx.output.line("ok output");
            Ok(())
        }
    }

    struct FailCommand;
    impl Command for FailCommand {
        fn name(&self) -> &str {
            "fail"
        }
        fn run(&self, _ctx: &mut CommandContext) -> CommandResult {
            Err(CommandError::failed("intentional"))
        }
    }

    struct SlowCommand;
    impl Command for SlowCommand {
        fn name(&self) -> &str {
            "slow"
        }
        fn run(&self, _ctx: &mut CommandContext) -> CommandResult {
            std::thread::sleep(std::time::Duration::from_secs(3));
            Ok(())
        }
    }

    struct OrderTrackingCommand {
        counter: Arc<AtomicUsize>,
        order_slot: Arc<std::sync::Mutex<Vec<(String, usize)>>>,
        label: String,
    }
    impl Command for OrderTrackingCommand {
        fn name(&self) -> &str {
            &self.label
        }
        fn run(&self, _ctx: &mut CommandContext) -> CommandResult {
            let n = self.counter.fetch_add(1, Ordering::SeqCst);
            self.order_slot
                .lock()
                .unwrap()
                .push((self.label.clone(), n));
            Ok(())
        }
    }

    #[tokio::test]
    async fn default_lifecycle_runs_command_ok() {
        // Arrange
        let ctx = make_ctx();
        let lifecycle = CommandLifecycle::default();
        let cmd: Arc<dyn Command> = Arc::new(OkCommand);

        // Act
        let result = run_with_lifecycle(
            cmd,
            ctx.settings.clone(),
            &lifecycle,
            &CancellationToken::new(),
        )
        .await;

        // Assert
        assert!(result.is_ok());
    }

    #[tokio::test]
    async fn the_outer_timer_returns_a_timeout_and_persists_nothing() {
        // Arrange
        let ctx = make_ctx();
        let dir = tempfile::tempdir().unwrap();
        let path = dir.path().join("output.txt");
        let lifecycle = CommandLifecycle {
            timeout: Some(Duration::from_secs(1)),
            write_output_to: Some(path.to_string_lossy().to_string()),
            ..Default::default()
        };
        let cmd: Arc<dyn Command> = Arc::new(SlowCommand);

        // Act
        let result = run_with_lifecycle(
            cmd,
            ctx.settings.clone(),
            &lifecycle,
            &CancellationToken::new(),
        )
        .await;

        // Assert
        assert!(matches!(
            result.unwrap_err(),
            CommandError::Timeout { seconds: 1 }
        ));
        assert!(!path.exists());
    }

    #[tokio::test]
    async fn write_output_to_writes_file() {
        // Arrange
        let ctx = make_ctx();
        let dir = tempfile::tempdir().unwrap();
        let path = dir.path().join("output.txt");
        let lifecycle = CommandLifecycle {
            write_output_to: Some(path.to_string_lossy().to_string()),
            ..Default::default()
        };
        let cmd: Arc<dyn Command> = Arc::new(OkCommand);

        // Act
        let result = run_with_lifecycle(
            cmd,
            ctx.settings.clone(),
            &lifecycle,
            &CancellationToken::new(),
        )
        .await;

        // Assert
        assert!(result.is_ok());
        let content = std::fs::read_to_string(&path).unwrap();
        assert!(content.contains("ok output"));
    }

    #[tokio::test]
    async fn append_output_to_appends_file() {
        // Arrange
        let ctx = make_ctx();
        let dir = tempfile::tempdir().unwrap();
        let path = dir.path().join("append.txt");
        std::fs::write(&path, "existing\n").unwrap();
        let lifecycle = CommandLifecycle {
            append_output_to: Some(path.to_string_lossy().to_string()),
            ..Default::default()
        };
        let cmd: Arc<dyn Command> = Arc::new(OkCommand);

        // Act
        let result = run_with_lifecycle(
            cmd,
            ctx.settings.clone(),
            &lifecycle,
            &CancellationToken::new(),
        )
        .await;

        // Assert
        assert!(result.is_ok());
        let content = std::fs::read_to_string(&path).unwrap();
        assert!(content.starts_with("existing\n"));
        assert!(content.contains("ok output"));
    }

    #[tokio::test]
    async fn before_run_called_before_command() {
        // Arrange
        let ctx = make_ctx();
        let counter = Arc::new(AtomicUsize::new(0));
        let order = Arc::new(std::sync::Mutex::new(Vec::<(String, usize)>::new()));

        let before_counter = counter.clone();
        let before_order = order.clone();
        let lifecycle = CommandLifecycle {
            before_run: Some(Box::new(move || {
                let n = before_counter.fetch_add(1, Ordering::SeqCst);
                before_order.lock().unwrap().push(("before".to_string(), n));
            })),
            ..Default::default()
        };

        let cmd: Arc<dyn Command> = Arc::new(OrderTrackingCommand {
            counter: counter.clone(),
            order_slot: order.clone(),
            label: "command".to_string(),
        });

        // Act
        let result = run_with_lifecycle(
            cmd,
            ctx.settings.clone(),
            &lifecycle,
            &CancellationToken::new(),
        )
        .await;

        // Assert
        assert!(result.is_ok());
        let recorded = order.lock().unwrap();
        assert_eq!(recorded[0].0, "before");
        assert_eq!(recorded[1].0, "command");
        assert!(recorded[0].1 < recorded[1].1);
    }

    #[tokio::test]
    async fn after_run_called_after_command() {
        // Arrange
        let ctx = make_ctx();
        let counter = Arc::new(AtomicUsize::new(0));
        let order = Arc::new(std::sync::Mutex::new(Vec::<(String, usize)>::new()));

        let after_counter = counter.clone();
        let after_order = order.clone();
        let lifecycle = CommandLifecycle {
            after_run: Some(Box::new(move || {
                let n = after_counter.fetch_add(1, Ordering::SeqCst);
                after_order.lock().unwrap().push(("after".to_string(), n));
            })),
            ..Default::default()
        };

        let cmd: Arc<dyn Command> = Arc::new(OrderTrackingCommand {
            counter: counter.clone(),
            order_slot: order.clone(),
            label: "command".to_string(),
        });

        // Act
        let result = run_with_lifecycle(
            cmd,
            ctx.settings.clone(),
            &lifecycle,
            &CancellationToken::new(),
        )
        .await;

        // Assert
        assert!(result.is_ok());
        let recorded = order.lock().unwrap();
        assert_eq!(recorded[0].0, "command");
        assert_eq!(recorded[1].0, "after");
    }

    #[tokio::test]
    async fn after_run_skipped_on_failure() {
        // Arrange
        let ctx = make_ctx();
        let after_called = Arc::new(AtomicUsize::new(0));
        let after_ref = after_called.clone();
        let lifecycle = CommandLifecycle {
            after_run: Some(Box::new(move || {
                after_ref.fetch_add(1, Ordering::SeqCst);
            })),
            ..Default::default()
        };
        let cmd: Arc<dyn Command> = Arc::new(FailCommand);

        // Act
        let result = run_with_lifecycle(
            cmd,
            ctx.settings.clone(),
            &lifecycle,
            &CancellationToken::new(),
        )
        .await;

        // Assert
        assert!(result.is_err());
        assert_eq!(after_called.load(Ordering::SeqCst), 0);
    }
}
