use std::future::Future;

use crate::command::{Command, CommandResult};
use crate::error::{CommandError, QueueError};
use crate::queue::delivery::{settle, Attempt, Delivery, SettleError, SettleOutcome};
use crate::queue::handle::Queue;
use crate::queue::registry::CommandRegistry;

/// What happened to one delivery: the attempt and the settle operation that followed it.
#[derive(Debug)]
pub struct ProcessedDelivery {
    pub message_id: String,
    pub attempt: AttemptReport,
    pub settled: Result<SettleOutcome, SettleError>,
}

/// The attempt result with the error that caused a failure.
#[derive(Debug)]
pub enum AttemptReport {
    Succeeded,
    CommandFailed(CommandError),
    Unrunnable(QueueError),
}

impl AttemptReport {
    pub fn attempt(&self) -> Attempt {
        match self {
            AttemptReport::Succeeded => Attempt::Succeeded,
            AttemptReport::CommandFailed(_) => Attempt::CommandFailed,
            AttemptReport::Unrunnable(_) => Attempt::Unrunnable,
        }
    }
}

/// Builds the command in `delivery` from `registry`, runs it with `run`, and settles the
/// delivery with the result.
pub async fn process_delivery<F, Fut>(
    queue: &Queue,
    delivery: Delivery,
    registry: &CommandRegistry,
    run: F,
) -> ProcessedDelivery
where
    F: FnOnce(Box<dyn Command>) -> Fut,
    Fut: Future<Output = CommandResult>,
{
    let message_id = delivery.envelope.id.clone();
    let attempt = match delivery.envelope.to_command(registry) {
        Ok(command) => match run(command).await {
            Ok(()) => AttemptReport::Succeeded,
            Err(error) => AttemptReport::CommandFailed(error),
        },
        Err(error) => AttemptReport::Unrunnable(error),
    };
    let settled = settle(queue, delivery, attempt.attempt()).await;
    ProcessedDelivery {
        message_id,
        attempt,
        settled,
    }
}
