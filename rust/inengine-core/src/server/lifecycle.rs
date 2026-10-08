use std::sync::Arc;
use std::time::Duration;

use crate::command::{Command, CommandContext, CommandResult};
use crate::config::MailSettings;
use crate::error::CommandError;
use crate::output::Output;

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

pub async fn run_with_lifecycle(
    cmd: Arc<dyn Command>,
    settings: Arc<crate::config::Settings>,
    lifecycle: &CommandLifecycle,
) -> CommandResult {
    if let Some(ref url) = lifecycle.ping_before_url {
        ping(url).await?;
    }

    if let Some(ref action) = lifecycle.before_run {
        action();
    }

    let result = if let Some(timeout) = lifecycle.timeout {
        let settings_clone = settings.clone();
        let cmd_clone = cmd.clone();

        let task = tokio::task::spawn_blocking(move || {
            let mut ctx = CommandContext {
                output: Output::new(false),
                settings: settings_clone,
            };
            let res = cmd_clone.run(&mut ctx);
            (res, ctx.output.take_buffer())
        });

        match tokio::time::timeout(timeout, task).await {
            Ok(Ok((res, output_text))) => (res, output_text),
            Ok(Err(e)) => {
                return Err(CommandError::failed_with("command task panicked", e));
            }
            Err(_elapsed) => {
                return Err(CommandError::Timeout {
                    seconds: timeout.as_secs(),
                });
            }
        }
    } else {
        let settings_clone = settings.clone();
        let cmd_clone = cmd.clone();
        let task = tokio::task::spawn_blocking(move || {
            let mut ctx = CommandContext {
                output: Output::new(false),
                settings: settings_clone,
            };
            let res = cmd_clone.run(&mut ctx);
            (res, ctx.output.take_buffer())
        });
        match task.await {
            Ok(pair) => pair,
            Err(e) => {
                return Err(CommandError::failed_with("command task panicked", e));
            }
        }
    };

    let (cmd_result, output_text) = result;
    cmd_result?;

    if let Some(ref action) = lifecycle.after_run {
        action();
    }

    if let Some(ref url) = lifecycle.ping_after_url {
        ping(url).await?;
    }

    if let Some(ref path) = lifecycle.write_output_to {
        std::fs::write(path, &output_text).map_err(|e| {
            CommandError::failed_with(format!("failed to write output to {path}"), e)
        })?;
    }

    if let Some(ref path) = lifecycle.append_output_to {
        use std::io::Write;
        let mut file = std::fs::OpenOptions::new()
            .create(true)
            .append(true)
            .open(path)
            .map_err(|e| {
                CommandError::failed_with(format!("failed to open {path} for append"), e)
            })?;
        file.write_all(output_text.as_bytes()).map_err(|e| {
            CommandError::failed_with(format!("failed to append output to {path}"), e)
        })?;
    }

    if let Some(ref address) = lifecycle.email_output_to {
        send_email(
            &settings.mail,
            address,
            &format!("Command completed: {}", cmd.name()),
            &output_text,
        )
        .await?;
    }

    Ok(())
}

pub async fn ping(url: &str) -> Result<(), CommandError> {
    let client = reqwest::Client::builder()
        .timeout(std::time::Duration::from_secs(30))
        .build()
        .map_err(|e| CommandError::failed_with("failed to create HTTP client", e))?;

    client
        .get(url)
        .send()
        .await
        .map_err(|e| CommandError::failed_with(format!("ping to {url} failed"), e))?;

    Ok(())
}

pub async fn send_email(
    mail_settings: &MailSettings,
    to: &str,
    subject: &str,
    body: &str,
) -> Result<(), CommandError> {
    use lettre::message::header::ContentType;
    use lettre::transport::smtp::authentication::Credentials;
    use lettre::{AsyncSmtpTransport, AsyncTransport, Message, Tokio1Executor};

    let email = Message::builder()
        .from(
            mail_settings
                .from
                .parse()
                .map_err(|e: lettre::address::AddressError| {
                    CommandError::failed_with(
                        format!("invalid from address: {}", mail_settings.from),
                        e,
                    )
                })?,
        )
        .to(to.parse().map_err(|e: lettre::address::AddressError| {
            CommandError::failed_with(format!("invalid to address: {to}"), e)
        })?)
        .subject(subject)
        .header(ContentType::TEXT_PLAIN)
        .body(body.to_string())
        .map_err(|e| CommandError::failed_with("failed to build email message", e))?;

    let mut transport_builder =
        AsyncSmtpTransport::<Tokio1Executor>::builder_dangerous(&mail_settings.host)
            .port(mail_settings.port);

    if !mail_settings.username.is_empty() {
        transport_builder = transport_builder.credentials(Credentials::new(
            mail_settings.username.clone(),
            mail_settings.password.clone(),
        ));
    }

    let transport = transport_builder.build();

    transport
        .send(email)
        .await
        .map_err(|e| CommandError::failed_with("failed to send email", e))?;

    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::command::{Command, CommandContext, CommandResult};
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
            std::thread::sleep(std::time::Duration::from_secs(10));
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

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn default_lifecycle_runs_command_ok() {
        // Arrange
        let ctx = make_ctx();
        let lifecycle = CommandLifecycle::default();
        let cmd: Arc<dyn Command> = Arc::new(OkCommand);

        // Act
        let result = run_with_lifecycle(cmd, ctx.settings.clone(), &lifecycle).await;

        // Assert
        assert!(result.is_ok());
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn timeout_expires_returns_timeout_error() {
        // Arrange
        let ctx = make_ctx();
        let lifecycle = CommandLifecycle {
            timeout: Some(Duration::from_secs(1)),
            ..Default::default()
        };
        let cmd: Arc<dyn Command> = Arc::new(SlowCommand);

        // Act
        let result = run_with_lifecycle(cmd, ctx.settings.clone(), &lifecycle).await;

        // Assert
        assert!(result.is_err());
        assert!(matches!(
            result.unwrap_err(),
            CommandError::Timeout { seconds: 1 }
        ));
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
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
        let result = run_with_lifecycle(cmd, ctx.settings.clone(), &lifecycle).await;

        // Assert
        assert!(result.is_ok());
        let content = std::fs::read_to_string(&path).unwrap();
        assert!(content.contains("ok output"));
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
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
        let result = run_with_lifecycle(cmd, ctx.settings.clone(), &lifecycle).await;

        // Assert
        assert!(result.is_ok());
        let content = std::fs::read_to_string(&path).unwrap();
        assert!(content.starts_with("existing\n"));
        assert!(content.contains("ok output"));
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
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
        let result = run_with_lifecycle(cmd, ctx.settings.clone(), &lifecycle).await;

        // Assert
        assert!(result.is_ok());
        let recorded = order.lock().unwrap();
        assert_eq!(recorded[0].0, "before");
        assert_eq!(recorded[1].0, "command");
        assert!(recorded[0].1 < recorded[1].1);
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
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
        let result = run_with_lifecycle(cmd, ctx.settings.clone(), &lifecycle).await;

        // Assert
        assert!(result.is_ok());
        let recorded = order.lock().unwrap();
        assert_eq!(recorded[0].0, "command");
        assert_eq!(recorded[1].0, "after");
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
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
        let result = run_with_lifecycle(cmd, ctx.settings.clone(), &lifecycle).await;

        // Assert
        assert!(result.is_err());
        assert_eq!(after_called.load(Ordering::SeqCst), 0);
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn ping_to_unreachable_returns_error() {
        // Arrange / Act
        let result = ping("http://192.0.2.1:1").await;

        // Assert
        assert!(result.is_err());
    }

    #[test]
    fn email_message_construction() {
        use lettre::message::header::ContentType;
        use lettre::Message;

        // Arrange / Act
        let email = Message::builder()
            .from("test@example.com".parse().unwrap())
            .to("dest@example.com".parse().unwrap())
            .subject("Test Subject")
            .header(ContentType::TEXT_PLAIN)
            .body("Test body".to_string());

        // Assert
        assert!(email.is_ok());
    }
}
