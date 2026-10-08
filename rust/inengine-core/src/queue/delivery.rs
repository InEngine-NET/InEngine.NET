use std::ffi::OsString;

use crate::error::QueueError;
use crate::queue::envelope::CommandEnvelope;
use crate::queue::handle::{Queue, QueueLane};

/// A message claimed from the pending stage, with the receipt that its driver uses to settle it.
///
/// Settle every delivery with [`settle`], or with [`Queue::acknowledge`], [`Queue::retry`],
/// or [`Queue::fail`]. A delivery that is dropped unsettled stays in the in-progress stage.
#[derive(Debug)]
#[must_use = "a dropped delivery stays in the in-progress stage"]
pub struct Delivery {
    pub envelope: CommandEnvelope,
    lane: QueueLane,
    receipt: Receipt,
}

#[derive(Debug)]
pub(crate) enum Receipt {
    /// The file name inside the in-progress directory.
    File(OsString),
    /// The exact bytes of the element that the claim returned.
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
    /// Returns the delivery from `Rejected` or `Interrupted`, and `None` for `Lost`.
    pub fn into_delivery(self) -> Option<Delivery> {
        match self {
            SettleError::Lost => None,
            SettleError::Rejected { delivery } | SettleError::Interrupted { delivery, .. } => {
                Some(*delivery)
            }
        }
    }
}

/// The result of running the command in a delivery.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Attempt {
    Succeeded,
    CommandFailed,
    /// The command could not be built from the registry, so a retry would fail the same way.
    Unrunnable,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum SettleAction {
    Acknowledge,
    Retry,
    MoveToFailed,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum SettleOutcome {
    Acknowledged,
    Retried,
    MovedToFailed,
}

/// Chooses what happens to a delivery after an attempt.
pub fn decide(attempt: Attempt, envelope: &CommandEnvelope) -> SettleAction {
    match attempt {
        Attempt::Succeeded => SettleAction::Acknowledge,
        Attempt::CommandFailed if envelope.should_retry() => SettleAction::Retry,
        Attempt::CommandFailed | Attempt::Unrunnable => SettleAction::MoveToFailed,
    }
}

/// Applies the action that [`decide`] chooses for the attempt.
pub async fn settle(
    queue: &Queue,
    delivery: Delivery,
    attempt: Attempt,
) -> Result<SettleOutcome, SettleError> {
    match decide(attempt, &delivery.envelope) {
        SettleAction::Acknowledge => queue
            .acknowledge(delivery)
            .await
            .map(|()| SettleOutcome::Acknowledged),
        SettleAction::Retry => queue.retry(delivery).await.map(|()| SettleOutcome::Retried),
        SettleAction::MoveToFailed => queue
            .fail(delivery)
            .await
            .map(|()| SettleOutcome::MovedToFailed),
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn envelope(current_try: u32, max_retries: u32) -> CommandEnvelope {
        let mut envelope =
            CommandEnvelope::from_fields("class", "plugin", "{}", false, max_retries)
                .expect("envelope");
        envelope.current_try = current_try;
        envelope
    }

    #[test]
    fn decide_follows_the_policy_table() {
        // Arrange
        let cases = [
            (
                Attempt::Succeeded,
                envelope(1, 0),
                SettleAction::Acknowledge,
            ),
            (
                Attempt::Succeeded,
                envelope(5, 2),
                SettleAction::Acknowledge,
            ),
            (Attempt::CommandFailed, envelope(1, 2), SettleAction::Retry),
            (Attempt::CommandFailed, envelope(2, 2), SettleAction::Retry),
            (
                Attempt::CommandFailed,
                envelope(3, 2),
                SettleAction::MoveToFailed,
            ),
            (
                Attempt::CommandFailed,
                envelope(1, 0),
                SettleAction::MoveToFailed,
            ),
            (
                Attempt::Unrunnable,
                envelope(1, 5),
                SettleAction::MoveToFailed,
            ),
        ];

        // Act
        let actions: Vec<_> = cases
            .iter()
            .map(|(attempt, envelope, _)| decide(*attempt, envelope))
            .collect();

        // Assert
        let expected: Vec<_> = cases.iter().map(|(_, _, action)| *action).collect();
        assert_eq!(actions, expected);
    }
}
