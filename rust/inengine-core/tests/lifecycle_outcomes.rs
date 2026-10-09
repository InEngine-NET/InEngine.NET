mod common;

use std::path::Path;
use std::sync::Arc;
use std::time::{Duration, Instant};

use common::default_settings;
use common::http::StatusServer;
use common::smtp::PlainSmtpServer;
use inengine_core::command::{Command, CommandContext, CommandResult};
use inengine_core::config::{MailEncryption, Settings};
use inengine_core::error::CommandError;
use inengine_core::server::lifecycle::{run_with_lifecycle, CommandLifecycle};
use inengine_core::server::notify::{ping, send_email};
use tokio_util::sync::CancellationToken;

struct WriteThenFail;

impl Command for WriteThenFail {
    fn name(&self) -> &str {
        "fail"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        ctx.output.line("partial output");
        Err(CommandError::failed("intentional"))
    }
}

struct WriteWithoutNewlineThenFail;

impl Command for WriteWithoutNewlineThenFail {
    fn name(&self) -> &str {
        "fail"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        ctx.output.text("partial");
        Err(CommandError::failed("intentional"))
    }
}

struct WriteThenSucceed;

impl Command for WriteThenSucceed {
    fn name(&self) -> &str {
        "succeed"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        ctx.output.line("all done");
        Ok(())
    }
}

struct Panics;

impl Command for Panics {
    fn name(&self) -> &str {
        "panics"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        ctx.output.line("partial output");
        panic!("intentional panic");
    }
}

struct FailWithCause;

impl Command for FailWithCause {
    fn name(&self) -> &str {
        "fail"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        ctx.output.line("partial output");
        Err(CommandError::failed_with(
            "intentional",
            std::io::Error::other("disk full"),
        ))
    }
}

struct WriteThenInterrupted;

impl Command for WriteThenInterrupted {
    fn name(&self) -> &str {
        "stopped"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        ctx.output.line("partial output");
        Err(CommandError::Interrupted)
    }
}

/// Writes a line, then waits for its deadline and reports the timeout, as a plugin call or a
/// process does.
struct HonorsDeadline;

impl Command for HonorsDeadline {
    fn name(&self) -> &str {
        "honors-deadline"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        ctx.output.line("started");
        let deadline = ctx.deadline.expect("the lifecycle sets a deadline");
        std::thread::sleep(deadline.at.saturating_duration_since(Instant::now()));
        Err(CommandError::Timeout {
            seconds: deadline.timeout.as_secs(),
        })
    }
}

fn settings_with_mail_server(port: u16, encryption: MailEncryption) -> Settings {
    let mut settings = default_settings();
    settings.mail.host = "127.0.0.1".to_string();
    settings.mail.port = port;
    settings.mail.from = "inengine@example.com".to_string();
    settings.mail.encryption = encryption;
    settings
}

fn output_file_lifecycle(path: &Path) -> CommandLifecycle {
    CommandLifecycle {
        write_output_to: Some(path.to_string_lossy().into_owned()),
        ..Default::default()
    }
}

fn mail_lifecycle() -> CommandLifecycle {
    CommandLifecycle {
        email_output_to: Some("ops@example.com".to_string()),
        ..Default::default()
    }
}

#[tokio::test]
async fn ping_fails_on_an_error_status() {
    // Arrange
    let server = StatusServer::start(500);

    // Act
    let result = ping(&server.url).await;

    // Assert
    let error = result.expect_err("a 500 response fails the ping");
    assert_eq!(
        error.to_string(),
        format!("command failed: ping to {} failed", server.url)
    );
}

#[tokio::test]
async fn a_failed_run_still_writes_its_output() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let path = dir.path().join("output.txt");
    let lifecycle = output_file_lifecycle(&path);
    let settings = Arc::new(default_settings());

    // Act
    let result = run_with_lifecycle(
        Arc::new(WriteThenFail),
        settings,
        &lifecycle,
        &CancellationToken::new(),
    )
    .await;

    // Assert
    let error = result.expect_err("the command fails");
    assert_eq!(error.to_string(), "command failed: intentional");
    let written = std::fs::read_to_string(&path).expect("the output file exists");
    assert_eq!(written, "partial output\n");
}

#[tokio::test]
async fn ping_succeeds_on_a_success_status() {
    // Arrange
    let server = StatusServer::start(200);

    // Act
    let result = ping(&server.url).await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
}

#[tokio::test]
async fn a_failed_run_appends_its_output() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let path = dir.path().join("append.txt");
    std::fs::write(&path, "existing\n").expect("write");
    let lifecycle = CommandLifecycle {
        append_output_to: Some(path.to_string_lossy().into_owned()),
        ..Default::default()
    };

    // Act
    let result = run_with_lifecycle(
        Arc::new(WriteThenFail),
        Arc::new(default_settings()),
        &lifecycle,
        &CancellationToken::new(),
    )
    .await;

    // Assert
    assert!(result.is_err());
    let written = std::fs::read_to_string(&path).expect("read");
    assert_eq!(written, "existing\npartial output\n");
}

#[tokio::test]
async fn a_failed_run_mails_its_output_and_error_chain() {
    // Arrange
    let server = PlainSmtpServer::start();
    let settings = settings_with_mail_server(server.port, MailEncryption::Plain);
    let lifecycle = mail_lifecycle();

    // Act
    let result = run_with_lifecycle(
        Arc::new(FailWithCause),
        Arc::new(settings),
        &lifecycle,
        &CancellationToken::new(),
    )
    .await;

    // Assert
    assert_eq!(
        result.expect_err("the command fails").to_string(),
        "command failed: intentional"
    );
    let mail = server.next_mail().expect("a mail arrives");
    assert!(
        mail.text.contains("Subject: Command failed: fail\r\n"),
        "{}",
        mail.text
    );
    let body = mail.text.replace("\r\n", "\n");
    assert!(
        body.trim_end().ends_with(
            "partial output\nError: command failed: intentional\n  caused by: disk full"
        ),
        "{body}"
    );
    assert!(mail
        .commands
        .iter()
        .any(|c| c == "MAIL FROM:<inengine@example.com>"));
    assert!(mail
        .commands
        .iter()
        .any(|c| c == "RCPT TO:<ops@example.com>"));
}

#[tokio::test]
async fn an_interrupted_run_persists_nothing() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let path = dir.path().join("output.txt");
    let lifecycle = output_file_lifecycle(&path);

    // Act
    let result = run_with_lifecycle(
        Arc::new(WriteThenInterrupted),
        Arc::new(default_settings()),
        &lifecycle,
        &CancellationToken::new(),
    )
    .await;

    // Assert
    assert!(
        matches!(result, Err(CommandError::Interrupted)),
        "{result:?}"
    );
    assert!(!path.exists());
}

#[tokio::test]
async fn a_command_timeout_persists_its_output() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let path = dir.path().join("output.txt");
    let lifecycle = CommandLifecycle {
        timeout: Some(Duration::from_secs(1)),
        ..output_file_lifecycle(&path)
    };

    // Act
    let result = run_with_lifecycle(
        Arc::new(HonorsDeadline),
        Arc::new(default_settings()),
        &lifecycle,
        &CancellationToken::new(),
    )
    .await;

    // Assert
    assert!(
        matches!(result, Err(CommandError::Timeout { seconds: 1 })),
        "{result:?}"
    );
    let written = std::fs::read_to_string(&path).expect("the output file exists");
    assert_eq!(written, "started\n");
}

#[tokio::test]
async fn starttls_refuses_a_server_without_starttls() {
    // Arrange
    let server = PlainSmtpServer::start();
    let settings = settings_with_mail_server(server.port, MailEncryption::StartTls);

    // Act
    let result = send_email(&settings.mail, "ops@example.com", "subject", "body").await;

    // Assert
    let error = result.expect_err("the server offers no STARTTLS");
    assert_eq!(error.to_string(), "command failed: failed to send email");
    let cause = std::error::Error::source(&error)
        .expect("a cause")
        .to_string();
    assert!(cause.contains("STARTTLS"), "{cause}");
}

#[tokio::test]
async fn tls_fails_against_a_plain_server() {
    // Arrange
    let server = PlainSmtpServer::start();
    let settings = settings_with_mail_server(server.port, MailEncryption::Tls);

    // Act
    let result = send_email(&settings.mail, "ops@example.com", "subject", "body").await;

    // Assert
    let error = result.expect_err("the server does not speak TLS");
    assert_eq!(error.to_string(), "command failed: failed to send email");
    let cause = std::error::Error::source(&error)
        .expect("a cause")
        .to_string();
    assert!(cause.starts_with("Connection error"), "{cause}");
    assert!(!cause.contains("STARTTLS"), "{cause}");
}

#[tokio::test]
async fn a_failed_run_mail_starts_the_error_on_its_own_line() {
    // Arrange
    let server = PlainSmtpServer::start();
    let settings = settings_with_mail_server(server.port, MailEncryption::Plain);
    let lifecycle = mail_lifecycle();

    // Act
    let result = run_with_lifecycle(
        Arc::new(WriteWithoutNewlineThenFail),
        Arc::new(settings),
        &lifecycle,
        &CancellationToken::new(),
    )
    .await;

    // Assert
    assert!(result.is_err());
    let mail = server.next_mail().expect("a mail arrives");
    let body = mail.text.replace("\r\n", "\n");
    assert!(
        body.trim_end()
            .ends_with("\n\npartial\nError: command failed: intentional"),
        "{body}"
    );
}

#[tokio::test]
async fn a_successful_run_mails_its_output() {
    // Arrange
    let server = PlainSmtpServer::start();
    let settings = settings_with_mail_server(server.port, MailEncryption::Plain);
    let lifecycle = mail_lifecycle();

    // Act
    let result = run_with_lifecycle(
        Arc::new(WriteThenSucceed),
        Arc::new(settings),
        &lifecycle,
        &CancellationToken::new(),
    )
    .await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    let mail = server.next_mail().expect("a mail arrives");
    let body = mail.text.replace("\r\n", "\n");
    assert!(
        body.contains("Subject: Command completed: succeed\n"),
        "{body}"
    );
    assert!(body.trim_end().ends_with("\n\nall done"), "{body}");
}

#[tokio::test]
async fn a_panicking_run_persists_nothing() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let path = dir.path().join("output.txt");
    let lifecycle = output_file_lifecycle(&path);

    // Act
    let result = run_with_lifecycle(
        Arc::new(Panics),
        Arc::new(default_settings()),
        &lifecycle,
        &CancellationToken::new(),
    )
    .await;

    // Assert
    let error = result.expect_err("the command panics");
    assert_eq!(error.to_string(), "command failed: the command panicked");
    let cause = std::error::Error::source(&error)
        .expect("a cause")
        .to_string();
    assert!(cause.contains("intentional panic"), "{cause}");
    assert!(!path.exists());
}

#[tokio::test]
async fn a_persistence_failure_does_not_replace_the_command_error() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let lifecycle = output_file_lifecycle(dir.path());

    // Act
    let result = run_with_lifecycle(
        Arc::new(WriteThenFail),
        Arc::new(default_settings()),
        &lifecycle,
        &CancellationToken::new(),
    )
    .await;

    // Assert
    let error = result.expect_err("the command fails");
    assert_eq!(error.to_string(), "command failed: intentional");
    assert!(dir.path().is_dir());
}
