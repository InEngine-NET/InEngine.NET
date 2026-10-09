use std::future::Future;

use tokio_util::sync::CancellationToken;

use crate::command::{Command, CommandResult};
use crate::error::{CommandError, QueueError};
use crate::queue::delivery::{settle, AttemptKind, Delivery, SettleError, SettleOutcome};
use crate::queue::registry::CommandRegistry;
use crate::queue::Queue;

#[derive(Debug)]
pub struct ProcessedDelivery {
    pub message_id: String,
    pub attempt: Attempt,
    pub settled: Result<SettleOutcome, SettleError>,
}

#[derive(Debug)]
pub enum Attempt {
    Succeeded,
    CommandFailed(CommandError),
    Unrunnable(QueueError),
    Interrupted,
}

impl Attempt {
    pub fn kind(&self) -> AttemptKind {
        match self {
            Attempt::Succeeded => AttemptKind::Succeeded,
            Attempt::CommandFailed(_) => AttemptKind::CommandFailed,
            Attempt::Unrunnable(_) => AttemptKind::Unrunnable,
            Attempt::Interrupted => AttemptKind::Interrupted,
        }
    }
}

/// An `Interrupted` result counts as an interruption only when `cancel` has fired. Otherwise
/// it is a failure, so a command that reports an interruption on its own cannot return to
/// pending forever without using its retries.
pub async fn process_delivery<F, Fut>(
    queue: &Queue,
    delivery: Delivery,
    registry: &CommandRegistry,
    cancel: &CancellationToken,
    run: F,
) -> ProcessedDelivery
where
    F: FnOnce(Box<dyn Command>) -> Fut,
    Fut: Future<Output = CommandResult>,
{
    let message_id = delivery.envelope.id.clone();
    let attempt = match delivery.envelope.to_command(registry) {
        Ok(command) => match run(command).await {
            Ok(()) => Attempt::Succeeded,
            Err(CommandError::Interrupted) if cancel.is_cancelled() => Attempt::Interrupted,
            Err(CommandError::Interrupted) => Attempt::CommandFailed(CommandError::failed(
                "the command stopped as if the server were stopping, but the server is running",
            )),
            Err(error) => Attempt::CommandFailed(error),
        },
        Err(error) => Attempt::Unrunnable(error),
    };
    let settled = settle(queue, delivery, attempt.kind()).await;
    ProcessedDelivery {
        message_id,
        attempt,
        settled,
    }
}

#[cfg(test)]
mod tests {
    use std::time::Duration;

    use super::*;
    use crate::config::settings_with_queue_base;
    use crate::queue::{CommandEnvelope, QueueLane};

    async fn queue_with_one_claimed_message(base: &std::path::Path) -> (Queue, Delivery) {
        let settings = settings_with_queue_base(base);
        let queue = Queue::open(QueueLane::Primary, &settings.queue)
            .await
            .expect("open");
        let envelope = CommandEnvelope::new("echo", "inengine-core", r#"{"text":"hi"}"#, false, 0)
            .expect("envelope");
        queue.publish(&envelope).await.expect("publish");
        let delivery = queue
            .dequeue(Duration::ZERO)
            .await
            .expect("dequeue")
            .expect("a message");
        (queue, delivery)
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn interrupted_command_while_the_server_runs_counts_as_a_failure() {
        // Arrange
        let dir = tempfile::tempdir().expect("temporary directory");
        let (queue, delivery) = queue_with_one_claimed_message(dir.path()).await;
        let registry = CommandRegistry::new();

        // Act
        let processed = process_delivery(
            &queue,
            delivery,
            &registry,
            &CancellationToken::new(),
            |_| async { Err(CommandError::Interrupted) },
        )
        .await;

        // Assert
        assert!(matches!(processed.attempt, Attempt::CommandFailed(_)));
        assert!(matches!(
            processed.settled,
            Ok(SettleOutcome::MovedToFailed)
        ));
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn interrupted_command_after_cancel_returns_to_pending_without_counting() {
        // Arrange
        let dir = tempfile::tempdir().expect("temporary directory");
        let (queue, delivery) = queue_with_one_claimed_message(dir.path()).await;
        let registry = CommandRegistry::new();
        let cancel = CancellationToken::new();
        cancel.cancel();

        // Act
        let processed = process_delivery(&queue, delivery, &registry, &cancel, |_| async {
            Err(CommandError::Interrupted)
        })
        .await;

        // Assert
        assert!(matches!(processed.attempt, Attempt::Interrupted));
        assert!(matches!(processed.settled, Ok(SettleOutcome::Released)));
        let reclaimed = queue
            .dequeue(Duration::ZERO)
            .await
            .expect("dequeue")
            .expect("the message is pending again");
        assert_eq!(reclaimed.envelope.attempts, 1);
    }
}
