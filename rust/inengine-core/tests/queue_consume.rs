//! Tests the publish, consume, and recover commands against a file queue.
mod common;

use std::path::Path;
use std::sync::Arc;
use std::time::Duration;

use common::{echo_envelope, expected_lengths, load_fixture_plugin, settings_with_queue_base};
use inengine_core::command::{Command, CommandContext, CommandResult};
use inengine_core::commands::exec::Exec;
use inengine_core::config::{PluginLimitsSettings, Settings};
use inengine_core::output::Output;
use inengine_core::queue::{
    CommandEnvelope, CommandRegistry, PeekedMessage, Queue, QueueLane, Stage,
};
use inengine_core::queue_admin::management::Recover;
use inengine_core::queue_admin::messaging::{Consume, Publish};

fn context(settings: Settings) -> CommandContext {
    CommandContext::new(Output::new(false), Arc::new(settings))
}

async fn open_primary(settings: &Settings) -> Queue {
    Queue::open(QueueLane::Primary, &settings.queue)
        .await
        .expect("open")
}

fn consume_one() -> Consume {
    Consume {
        limit: Some(1),
        lane: QueueLane::Primary,
        registry: Arc::new(CommandRegistry::new()),
    }
}

fn publish_echo_command(max_retries: Option<u32>) -> Publish {
    Publish {
        plugin_name: "inengine-core".into(),
        command_name: "echo".into(),
        args: vec!["text=hello".into()],
        json: None,
        max_retries,
        lane: QueueLane::Primary,
    }
}

async fn published_max_retries(queue: &Queue) -> Vec<u32> {
    queue
        .peek(Stage::Pending, 0..=9)
        .await
        .expect("peek")
        .into_iter()
        .map(|message| match message {
            PeekedMessage::Envelope(envelope) => envelope.max_retries,
            PeekedMessage::Malformed { reason, .. } => panic!("malformed: {reason}"),
        })
        .collect()
}

fn settings_with_retries(base: &Path, max_retries: u32) -> Settings {
    let mut settings = settings_with_queue_base(base);
    settings.queue.max_retries = max_retries;
    settings
}

#[tokio::test]
async fn consume_acknowledges_a_successful_message() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let settings = settings_with_queue_base(dir.path());
    let queue = open_primary(&settings).await;
    let envelope = echo_envelope("hi", 0);
    queue.publish(&envelope).await.expect("publish");
    let mut ctx = context(settings);

    // Act
    let result = consume_one()
        .run(ctx.settings.clone(), &mut ctx.output)
        .await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 0, 0)
    );
    assert_eq!(
        ctx.output.take_buffer(),
        format!("hi\n{}: completed\n", envelope.id)
    );
}

#[tokio::test]
async fn consume_of_a_failing_exec_message_moves_it_to_failed_and_returns_an_error() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let mut settings = settings_with_queue_base(dir.path());
    settings
        .exec_whitelist
        .insert("false".into(), "/usr/bin/false".into());
    let queue = open_primary(&settings).await;
    let exec = Exec {
        executable: "false".into(),
        arguments: Vec::new(),
        timeout: 5,
    };
    let envelope = CommandEnvelope::from_command(&exec, false, 0).expect("envelope");
    queue.publish(&envelope).await.expect("publish");
    let mut ctx = context(settings);

    // Act
    let result = consume_one()
        .run(ctx.settings.clone(), &mut ctx.output)
        .await;

    // Assert
    let error = result.expect_err("a failed command fails the consume");
    assert_eq!(
        error.to_string(),
        "command failed: some messages did not complete \
         (completed: 0, returned for a retry: 0, moved to failed: 1)"
    );
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 0, 1)
    );
    assert_eq!(
        ctx.output.take_buffer(),
        format!(
            "message {id}: command failed: /usr/bin/false exited with code 1\n\
             {id}: moved to failed\n",
            id = envelope.id
        )
    );
}

#[tokio::test]
async fn consume_of_an_unknown_command_moves_it_to_failed() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let settings = settings_with_queue_base(dir.path());
    let queue = open_primary(&settings).await;
    let envelope = CommandEnvelope::new("missing", "nowhere", "{}", false, 3).expect("envelope");
    queue.publish(&envelope).await.expect("publish");
    let mut ctx = context(settings);

    // Act
    let result = consume_one()
        .run(ctx.settings.clone(), &mut ctx.output)
        .await;

    // Assert
    assert!(result.is_err());
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 0, 1)
    );
    assert_eq!(
        ctx.output.take_buffer(),
        format!(
            "could not build the command for message {id}: \
             command not found in plugin 'nowhere': missing\n{id}: moved to failed\n",
            id = envelope.id
        )
    );
}

#[tokio::test]
async fn publish_stores_max_retries_from_the_settings() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let settings = settings_with_retries(dir.path(), 4);
    let queue = open_primary(&settings).await;
    let mut ctx = context(settings);

    // Act
    let result = publish_echo_command(None)
        .run(&ctx.settings.queue, &mut ctx.output)
        .await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    assert_eq!(published_max_retries(&queue).await, vec![4]);
}

#[tokio::test]
async fn publish_with_max_retries_overrides_the_setting() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let settings = settings_with_retries(dir.path(), 4);
    let queue = open_primary(&settings).await;
    let mut ctx = context(settings);

    // Act
    let result = publish_echo_command(Some(1))
        .run(&ctx.settings.queue, &mut ctx.output)
        .await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    assert_eq!(published_max_retries(&queue).await, vec![1]);
}

#[tokio::test]
async fn a_message_published_with_json_runs_with_that_payload() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let mut settings = settings_with_queue_base(dir.path());
    settings
        .exec_whitelist
        .insert("echo".into(), "/bin/echo".into());
    let queue = open_primary(&settings).await;
    let mut ctx = context(settings);
    let publish = Publish {
        json: Some(r#"{"executable":"echo","arguments":["from", "json"],"timeout":5}"#.into()),
        command_name: "exec".into(),
        args: Vec::new(),
        ..publish_echo_command(None)
    };
    publish
        .run(&ctx.settings.queue, &mut ctx.output)
        .await
        .expect("publish");
    ctx.output.take_buffer();

    // Act
    let result = consume_one()
        .run(ctx.settings.clone(), &mut ctx.output)
        .await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    assert!(ctx.output.take_buffer().starts_with("from json\n"));
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 0, 0)
    );
}

#[tokio::test]
async fn the_recover_command_returns_in_progress_messages_to_pending() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let settings = settings_with_queue_base(dir.path());
    let queue = open_primary(&settings).await;
    queue
        .publish(&echo_envelope("x", 0))
        .await
        .expect("publish");
    drop(queue.dequeue(Duration::ZERO).await.expect("dequeue"));
    let mut ctx = context(settings);
    let recover = Recover {
        lane: QueueLane::Primary,
    };

    // Act
    let result = recover.run(&ctx.settings.queue, &mut ctx.output).await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(1, 0, 0)
    );
    assert_eq!(
        ctx.output.take_buffer(),
        "in-progress messages returned to pending in the Primary queue: 1\n"
    );
}

#[tokio::test]
async fn consume_runs_a_queued_plugin_command() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let settings = settings_with_queue_base(dir.path());
    let queue = open_primary(&settings).await;
    let loader = load_fixture_plugin(PluginLimitsSettings::default());
    let mut registry = CommandRegistry::new();
    assert!(loader.register_commands(&mut registry).is_empty());
    let envelope =
        CommandEnvelope::new("echo-args", "fixture", r#"{"a":"b"}"#, false, 0).expect("envelope");
    queue.publish(&envelope).await.expect("publish");
    let consume = Consume {
        registry: Arc::new(registry),
        ..consume_one()
    };
    let mut ctx = context(settings);

    // Act
    let result = consume.run(ctx.settings.clone(), &mut ctx.output).await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    assert_eq!(
        ctx.output.take_buffer(),
        format!("{{\"a\":\"b\"}}\n{}: completed\n", envelope.id)
    );
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 0, 0)
    );
}

#[tokio::test]
async fn consume_writes_each_command_output_in_order() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let settings = settings_with_queue_base(dir.path());
    let queue = open_primary(&settings).await;
    let first = echo_envelope("first", 0);
    let second = echo_envelope("second", 0);
    queue.publish(&first).await.expect("publish");
    queue.publish(&second).await.expect("publish");
    let consume = Consume {
        limit: Some(2),
        ..consume_one()
    };
    let mut ctx = context(settings);

    // Act
    let result = consume.run(ctx.settings.clone(), &mut ctx.output).await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    assert_eq!(
        ctx.output.take_buffer(),
        format!(
            "first\n{}: completed\nsecond\n{}: completed\n",
            first.id, second.id
        )
    );
}

struct Panics;

impl Command for Panics {
    fn name(&self) -> &str {
        "panics"
    }

    fn run(&self, _ctx: &mut CommandContext) -> CommandResult {
        panic!("intentional panic");
    }
}

#[tokio::test]
async fn consume_of_a_panicking_command_moves_it_to_failed() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let settings = settings_with_queue_base(dir.path());
    let queue = open_primary(&settings).await;
    let mut registry = CommandRegistry::new();
    registry
        .register("test", "panics", |_json| Ok(Box::new(Panics)))
        .expect("register");
    let envelope = CommandEnvelope::new("panics", "test", "{}", false, 0).expect("envelope");
    queue.publish(&envelope).await.expect("publish");
    let consume = Consume {
        registry: Arc::new(registry),
        ..consume_one()
    };
    let mut ctx = context(settings);

    // Act
    let result = consume.run(ctx.settings.clone(), &mut ctx.output).await;

    // Assert
    assert!(result.is_err());
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 0, 1)
    );
    let printed = ctx.output.take_buffer();
    let lines: Vec<&str> = printed.lines().collect();
    assert_eq!(lines.len(), 2, "{printed}");
    let failure = format!(
        "message {}: command failed: the command panicked: ",
        envelope.id
    );
    assert!(lines[0].starts_with(&failure), "{printed}");
    assert!(lines[0].ends_with("\"intentional panic\""), "{printed}");
    assert_eq!(lines[1], format!("{}: moved to failed", envelope.id));
}
