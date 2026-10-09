use std::ffi::OsString;

use crate::error::QueueError;
use crate::queue::envelope::CommandEnvelope;
use crate::queue::{Queue, QueueLane};

#[derive(Debug)]
#[must_use = "a dropped delivery stays in the in-progress stage"]
pub struct Delivery {
    pub envelope: CommandEnvelope,
    lane: QueueLane,
    receipt: Receipt,
}

#[derive(Debug)]
pub(crate) enum Receipt {
    File(OsString),
    /// The exact bytes the claim returned, because Redis removes an element only by an exact match.
    Redis(Vec<u8>),
}

impl Delivery {
    pub(crate) fn new(envelope: CommandEnvelope, lane: QueueLane, receipt: Receipt) -> Self {
        Self {
            envelope,
            lane,
            receipt,
        }
    }

    pub fn lane(&self) -> QueueLane {
        self.lane
    }

    pub(crate) fn receipt(&self) -> &Receipt {
        &self.receipt
    }
}

#[derive(Debug, thiserror::Error)]
pub enum SettleError {
    /// Another operation, such as a flush or a recovery, removed the message from the
    /// in-progress stage before this settle. Nothing was written.
    #[error("the message is no longer in progress")]
    Lost,

    /// The delivery came from a different lane or driver. Nothing was written, and another
    /// settle on this queue fails the same way.
    #[error("message {} belongs to a different queue lane or driver", delivery.envelope.id)]
    Rejected { delivery: Box<Delivery> },

    /// The settle failed partway, for example on a connection error. The message is still in
    /// progress, so settle the returned delivery again.
    #[error("could not settle message {}", delivery.envelope.id)]
    Interrupted {
        delivery: Box<Delivery>,
        #[source]
        source: QueueError,
    },
}

impl SettleError {
    pub fn into_delivery(self) -> Option<Delivery> {
        match self {
            SettleError::Lost => None,
            SettleError::Rejected { delivery } | SettleError::Interrupted { delivery, .. } => {
                Some(*delivery)
            }
        }
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum AttemptKind {
    Succeeded,
    CommandFailed,
    /// The command could not be built from the registry, so a retry would fail the same way.
    Unrunnable,
    /// The server stopped the command before it finished, so the attempt does not count.
    Interrupted,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum SettleAction {
    Acknowledge,
    Retry,
    MoveToFailed,
    /// Return the message to pending without counting the attempt.
    Release,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum SettleOutcome {
    Acknowledged,
    Retried,
    MovedToFailed,
    Released,
}

impl SettleAction {
    pub fn for_attempt(kind: AttemptKind, envelope: &CommandEnvelope) -> Self {
        match kind {
            AttemptKind::Succeeded => Self::Acknowledge,
            AttemptKind::CommandFailed if envelope.has_retries_left() => Self::Retry,
            AttemptKind::CommandFailed | AttemptKind::Unrunnable => Self::MoveToFailed,
            AttemptKind::Interrupted => Self::Release,
        }
    }
}

impl SettleOutcome {
    pub fn label(self) -> &'static str {
        match self {
            SettleOutcome::Acknowledged => "completed",
            SettleOutcome::Retried => "returned to pending for a retry",
            SettleOutcome::MovedToFailed => "moved to failed",
            SettleOutcome::Released => "returned to pending because the server is stopping",
        }
    }
}

pub async fn settle(
    queue: &Queue,
    delivery: Delivery,
    kind: AttemptKind,
) -> Result<SettleOutcome, SettleError> {
    match SettleAction::for_attempt(kind, &delivery.envelope) {
        SettleAction::Acknowledge => queue
            .acknowledge(delivery)
            .await
            .map(|()| SettleOutcome::Acknowledged),
        SettleAction::Retry => queue.retry(delivery).await.map(|()| SettleOutcome::Retried),
        SettleAction::MoveToFailed => queue
            .move_to_failed(delivery)
            .await
            .map(|()| SettleOutcome::MovedToFailed),
        SettleAction::Release => queue
            .release(delivery)
            .await
            .map(|()| SettleOutcome::Released),
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn envelope(attempts: u32, max_retries: u32) -> CommandEnvelope {
        let mut envelope =
            CommandEnvelope::new("class", "plugin", "{}", false, max_retries).expect("envelope");
        envelope.attempts = attempts;
        envelope
    }

    #[test]
    fn for_attempt_follows_the_policy_table() {
        // Arrange
        let cases = [
            (
                AttemptKind::Succeeded,
                envelope(1, 0),
                SettleAction::Acknowledge,
            ),
            (
                AttemptKind::Succeeded,
                envelope(5, 2),
                SettleAction::Acknowledge,
            ),
            (
                AttemptKind::CommandFailed,
                envelope(1, 2),
                SettleAction::Retry,
            ),
            (
                AttemptKind::CommandFailed,
                envelope(2, 2),
                SettleAction::Retry,
            ),
            (
                AttemptKind::CommandFailed,
                envelope(3, 2),
                SettleAction::MoveToFailed,
            ),
            (
                AttemptKind::CommandFailed,
                envelope(1, 0),
                SettleAction::MoveToFailed,
            ),
            (
                AttemptKind::Unrunnable,
                envelope(1, 5),
                SettleAction::MoveToFailed,
            ),
            (
                AttemptKind::Interrupted,
                envelope(1, 0),
                SettleAction::Release,
            ),
            (
                AttemptKind::Interrupted,
                envelope(3, 2),
                SettleAction::Release,
            ),
        ];

        // Act
        let actions: Vec<_> = cases
            .iter()
            .map(|(attempt, envelope, _)| SettleAction::for_attempt(*attempt, envelope))
            .collect();

        // Assert
        let expected: Vec<_> = cases.iter().map(|(_, _, action)| *action).collect();
        assert_eq!(actions, expected);
    }
}
