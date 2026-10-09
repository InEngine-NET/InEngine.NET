//! Tests queue administration on a current-thread runtime.
mod common;

use common::{expected_lengths, settings_with_queue_base};
use inengine_core::output::Output;
use inengine_core::queue::{Queue, QueueLane};
use inengine_core::queue_admin::management::write_lengths;
use inengine_core::queue_admin::messaging::Publish;

/// Queue administration must work on a current-thread runtime, which a library caller may
/// use.
#[tokio::test]
async fn queue_admin_runs_on_a_current_thread_runtime() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let settings = settings_with_queue_base(dir.path());
    let publish = Publish {
        plugin_name: "inengine-core".into(),
        command_name: "echo".into(),
        args: vec!["text=hello".into()],
        json: None,
        max_retries: None,
        lane: QueueLane::Primary,
    };
    let mut output = Output::new(false);
    publish
        .run(&settings.queue, &mut output)
        .await
        .expect("publish");
    output.take_buffer();

    // Act
    let result = write_lengths(&settings.queue, &mut output).await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    let queue = Queue::open(QueueLane::Primary, &settings.queue)
        .await
        .expect("open");
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(1, 0, 0)
    );
    assert_eq!(
        output.take_buffer(),
        "Primary Queue:\n       pending         1\n   in-progress         0\n        failed         0\n\n\
         Secondary Queue:\n       pending         0\n   in-progress         0\n        failed         0\n\n"
    );
}
