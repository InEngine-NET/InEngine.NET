use std::future::Future;

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
}

impl Attempt {
    pub fn kind(&self) -> AttemptKind {
        match self {
            Attempt::Succeeded => AttemptKind::Succeeded,
            Attempt::CommandFailed(_) => AttemptKind::CommandFailed,
            Attempt::Unrunnable(_) => AttemptKind::Unrunnable,
        }
    }
}

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
            Ok(()) => Attempt::Succeeded,
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
