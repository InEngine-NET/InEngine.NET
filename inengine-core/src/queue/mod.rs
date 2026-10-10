mod compression;
mod delivery;
mod driver;
mod envelope;
mod processing;
mod registry;

pub(crate) use delivery::{AttemptKind, SettleOutcome, settle};
pub use delivery::{Delivery, SettleError};
pub use envelope::CommandEnvelope;
pub(crate) use processing::{Attempt, process_delivery};
pub use registry::CommandRegistry;

use std::ops::RangeInclusive;
use std::time::Duration;

use crate::config::{QueueDriver, QueueSettings};
use crate::error::QueueError;
use crate::queue::delivery::Receipt;
use crate::queue::driver::file::FileDriver;
use crate::queue::driver::redis::RedisDriver;

/// One of the two independent queues that share a queue name.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum QueueLane {
    /// The lane that `--secondary` does not select. The server runs `primary_queue_consumers`
    /// consumers on it.
    Primary,
    /// The lane that `--secondary` selects. The server runs `secondary_queue_consumers`
    /// consumers on it.
    Secondary,
}

impl QueueLane {
    /// Both lanes, primary first. Use it when you act on every lane of a queue name.
    pub const ALL: [QueueLane; 2] = [QueueLane::Primary, QueueLane::Secondary];

    /// The name used in Redis keys and directory names.
    pub fn as_str(self) -> &'static str {
        match self {
            QueueLane::Primary => "Primary",
            QueueLane::Secondary => "Secondary",
        }
    }
}

/// A step in the life of a queued message. Each lane keeps a separate list of messages for
/// each stage.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Stage {
    /// Messages that wait for a consumer to claim them.
    Pending,
    /// Messages that a consumer has claimed and not yet settled. A message stays here after a
    /// crash until you recover the queue.
    InProgress,
    /// Messages that ran out of retries or could not be read or built. They stay here until you
    /// republish or clear them.
    Failed,
}

impl Stage {
    /// All three stages, in the order a message moves through them.
    pub const ALL: [Stage; 3] = [Stage::Pending, Stage::InProgress, Stage::Failed];

    /// The name used in Redis keys and directory names.
    pub fn as_str(self) -> &'static str {
        match self {
            Stage::Pending => "Pending",
            Stage::InProgress => "InProgress",
            Stage::Failed => "Failed",
        }
    }

    /// The name shown to an operator.
    pub fn label(self) -> &'static str {
        match self {
            Stage::Pending => "pending",
            Stage::InProgress => "in-progress",
            Stage::Failed => "failed",
        }
    }
}

/// The number of messages in each stage of one lane at the time of the read.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct QueueLengths {
    /// The count of messages that wait to be claimed.
    pub pending: usize,
    /// The count of messages that a consumer has claimed and not yet settled.
    pub in_progress: usize,
    /// The count of messages in the failed stage.
    pub failed: usize,
}

/// One entry read by [`Queue::peek`]. Peeking does not claim or change the entry.
#[derive(Debug, Clone)]
pub enum PeekedMessage {
    /// An entry that parses as a message.
    Envelope(CommandEnvelope),
    /// An entry that does not parse. `raw` holds its text, with invalid UTF-8 replaced.
    Malformed {
        /// The stored text of the entry, so that you can inspect or repair it by hand.
        raw: String,
        /// The parser error that tells why the entry does not parse.
        reason: String,
    },
}

impl PeekedMessage {
    pub(crate) fn parse(raw: &[u8]) -> Self {
        match serde_json::from_slice(raw) {
            Ok(envelope) => PeekedMessage::Envelope(envelope),
            Err(error) => PeekedMessage::Malformed {
                raw: String::from_utf8_lossy(raw).into_owned(),
                reason: error.to_string(),
            },
        }
    }
}

/// A connection to one lane of a queue on the configured driver.
///
/// Give each consumer its own `Queue`. A Redis queue has one connection, and a blocking
/// dequeue delays every other request on that connection.
pub struct Queue {
    backend: Backend,
}

enum Backend {
    File(FileDriver),
    Redis(RedisDriver),
}

impl Queue {
    /// Connects to the queue for `lane` with the driver that `settings` selects. The file
    /// driver creates its stage directories if they are missing.
    pub async fn open(lane: QueueLane, settings: &QueueSettings) -> Result<Queue, QueueError> {
        let backend = match settings.queue_driver {
            QueueDriver::File => Backend::File(
                FileDriver::open(&settings.queue_name, lane, &settings.file.base_path).await?,
            ),
            QueueDriver::Redis => Backend::Redis(
                RedisDriver::open(&settings.queue_name, lane, &settings.redis).await?,
            ),
        };
        Ok(Queue { backend })
    }

    /// Returns the lane that this queue reads and writes.
    pub fn lane(&self) -> QueueLane {
        match &self.backend {
            Backend::File(driver) => driver.lane(),
            Backend::Redis(driver) => driver.lane(),
        }
    }

    /// Adds the message to the end of the pending stage. Its `attempts` value is stored as
    /// you give it.
    pub async fn publish(&self, envelope: &CommandEnvelope) -> Result<(), QueueError> {
        match &self.backend {
            Backend::File(driver) => driver.publish(envelope).await,
            Backend::Redis(driver) => driver.publish(envelope).await,
        }
    }

    /// Claims the oldest pending message and counts an attempt, or waits up to `wait` for one.
    /// Returns `None` when no message arrives in time. An element that does not parse is moved
    /// to the failed stage and skipped.
    ///
    /// This future is not cancel safe. If it is dropped, the driver can still claim a message
    /// that no caller receives, so let it finish. It finishes within `wait`.
    pub async fn dequeue(&self, wait: Duration) -> Result<Option<Delivery>, QueueError> {
        match &self.backend {
            Backend::File(driver) => driver.dequeue(wait).await,
            Backend::Redis(driver) => driver.dequeue(wait).await,
        }
    }

    /// Removes a claimed message after it ran successfully. Fails with
    /// [`SettleError::Rejected`] when the delivery came from another lane or driver.
    pub async fn acknowledge(&self, delivery: Delivery) -> Result<(), SettleError> {
        let settled = match (&self.backend, delivery.receipt()) {
            (Backend::File(driver), Receipt::File(name)) if self.owns(&delivery) => {
                driver.acknowledge(name).await
            }
            (Backend::Redis(driver), Receipt::Redis(element)) if self.owns(&delivery) => {
                driver.acknowledge(element).await
            }
            _ => return Err(rejected(delivery)),
        };
        settle_result(delivery, settled)
    }

    /// Returns a claimed message to the end of the pending stage, with its new attempt count
    /// stored.
    pub async fn retry(&self, delivery: Delivery) -> Result<(), SettleError> {
        self.move_claimed(delivery, Stage::Pending).await
    }

    /// Moves a claimed message to the failed stage, with its new attempt count stored.
    pub async fn move_to_failed(&self, delivery: Delivery) -> Result<(), SettleError> {
        self.move_claimed(delivery, Stage::Failed).await
    }

    /// Returns the message to pending and takes back the attempt that the claim counted.
    pub async fn release(&self, mut delivery: Delivery) -> Result<(), SettleError> {
        delivery.envelope.attempts = delivery.envelope.attempts.saturating_sub(1);
        self.move_claimed(delivery, Stage::Pending).await
    }

    /// Returns every in-progress message to pending, oldest claim first, and returns how many
    /// it moved.
    ///
    /// Stop every other consumer of the queue first. Recovery cannot tell a message left by a
    /// crash from one that another consumer is still processing, so that message would run
    /// twice.
    pub async fn recover(&self) -> Result<usize, QueueError> {
        match &self.backend {
            Backend::File(driver) => driver.recover().await,
            Backend::Redis(driver) => driver.recover().await,
        }
    }

    /// Counts the messages in each stage of this lane.
    pub async fn lengths(&self) -> Result<QueueLengths, QueueError> {
        match &self.backend {
            Backend::File(driver) => driver.lengths().await,
            Backend::Redis(driver) => driver.lengths().await,
        }
    }

    /// Removes every message in `stage` and returns how many it removed. Clearing the
    /// in-progress stage while a consumer runs makes that consumer's settle fail with
    /// [`SettleError::Lost`].
    pub async fn clear(&self, stage: Stage) -> Result<usize, QueueError> {
        match &self.backend {
            Backend::File(driver) => driver.clear(stage).await,
            Backend::Redis(driver) => driver.clear(stage).await,
        }
    }

    /// Reads the messages at the positions in `range` of `stage` without moving them. Position 0
    /// of the pending stage is the next message to be claimed. A reversed range reads nothing.
    pub async fn peek(
        &self,
        stage: Stage,
        range: RangeInclusive<usize>,
    ) -> Result<Vec<PeekedMessage>, QueueError> {
        if range.is_empty() {
            return Ok(Vec::new());
        }
        let (first, last) = range.into_inner();
        match &self.backend {
            Backend::File(driver) => driver.peek(stage, first, last).await,
            Backend::Redis(driver) => driver.peek(stage, first, last).await,
        }
    }

    /// Moves up to `limit` messages from the failed stage to the end of the pending stage,
    /// oldest first, and returns how many it moved. Each message keeps its attempt count.
    pub async fn republish_failed(&self, limit: usize) -> Result<usize, QueueError> {
        match &self.backend {
            Backend::File(driver) => driver.republish_failed(limit).await,
            Backend::Redis(driver) => driver.republish_failed(limit).await,
        }
    }

    async fn move_claimed(&self, delivery: Delivery, target: Stage) -> Result<(), SettleError> {
        let settled = match (&self.backend, delivery.receipt()) {
            (Backend::File(driver), Receipt::File(name)) if self.owns(&delivery) => {
                driver.move_claimed(name, &delivery.envelope, target).await
            }
            (Backend::Redis(driver), Receipt::Redis(element)) if self.owns(&delivery) => {
                driver
                    .move_claimed(element, &delivery.envelope, target)
                    .await
            }
            _ => return Err(rejected(delivery)),
        };
        settle_result(delivery, settled)
    }

    fn owns(&self, delivery: &Delivery) -> bool {
        delivery.lane() == self.lane()
    }
}

fn rejected(delivery: Delivery) -> SettleError {
    SettleError::Rejected {
        delivery: Box::new(delivery),
    }
}

/// `Ok(false)` means the message was no longer in progress.
fn settle_result(delivery: Delivery, settled: Result<bool, QueueError>) -> Result<(), SettleError> {
    match settled {
        Ok(true) => Ok(()),
        Ok(false) => Err(SettleError::Lost),
        Err(source) => Err(SettleError::Interrupted {
            delivery: Box::new(delivery),
            source,
        }),
    }
}
