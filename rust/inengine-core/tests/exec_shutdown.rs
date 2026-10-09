mod common;

use std::sync::Arc;
use std::time::Duration;

use common::{expected_lengths, settings_with_queue_base};
use inengine_core::commands::exec::Exec;
use inengine_core::queue::{
    CommandEnvelope, CommandRegistry, PeekedMessage, Queue, QueueLane, Stage,
};
use inengine_core::scheduling::schedule::Schedule;
use inengine_core::server::host::Server;
use tokio_util::sync::CancellationToken;

const STOP_WAIT_LIMIT: Duration = Duration::from_secs(10);

#[tokio::test(flavor = "multi_thread", worker_threads = 4)]
async fn a_queued_exec_returns_to_pending_when_the_server_stops() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let mut settings = settings_with_queue_base(dir.path());
    settings.queue.primary_queue_consumers = 1;
    settings.queue.secondary_queue_consumers = 0;
    settings
        .exec_whitelist
        .insert("sleep".to_string(), "/bin/sleep".to_string());
    let queue = Queue::open(QueueLane::Primary, &settings.queue)
        .await
        .expect("open");
    let exec = Exec {
        executable: "sleep".to_string(),
        arguments: vec!["60".to_string()],
        timeout: 0,
    };
    let envelope = CommandEnvelope::from_command(&exec, false, 0).expect("envelope");
    queue.publish(&envelope).await.expect("publish");
    let server = Server::new(
        Arc::new(settings),
        Arc::new(CommandRegistry::new()),
        Schedule::new(),
    );
    let cancel = CancellationToken::new();
    let running = tokio::spawn(server.run(cancel.clone()));
    let claimed = expected_lengths(0, 1, 0);
    tokio::time::timeout(STOP_WAIT_LIMIT, async {
        while queue.lengths().await.expect("lengths") != claimed {
            tokio::time::sleep(Duration::from_millis(50)).await;
        }
    })
    .await
    .expect("a consumer claims the message");

    // Act
    cancel.cancel();

    // Assert
    tokio::time::timeout(STOP_WAIT_LIMIT, running)
        .await
        .expect("the server stops within the wait limit")
        .expect("the server task ends cleanly");
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(1, 0, 0)
    );
    let pending = queue.peek(Stage::Pending, 0..=0).await.expect("peek");
    match &pending[..] {
        [PeekedMessage::Envelope(message)] => {
            assert_eq!(message.id, envelope.id);
            assert_eq!(message.attempts, 0);
        }
        other => panic!("expected the exec message, got {other:?}"),
    }
}
