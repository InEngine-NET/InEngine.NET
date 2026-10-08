use std::ops::RangeInclusive;
use std::time::Duration;

use crate::config::{QueueDriver, QueueSettings};
use crate::error::QueueError;
use crate::queue::delivery::{Delivery, Receipt, SettleError};
use crate::queue::driver::file::FileDriver;
use crate::queue::driver::redis::RedisDriver;
use crate::queue::envelope::CommandEnvelope;

/// One of the two independent queues that share a queue name.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum QueueLane {
    Primary,
    Secondary,
}

impl QueueLane {
    pub const ALL: [QueueLane; 2] = [QueueLane::Primary, QueueLane::Secondary];

    /// The name used in Redis keys and directory names.
    pub fn as_str(self) -> &'static str {
        match self {
            QueueLane::Primary => "Primary",
            QueueLane::Secondary => "Secondary",
        }
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Stage {
    Pending,
    InProgress,
    Failed,
}

impl Stage {
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

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct QueueLengths {
    pub pending: usize,
    pub in_progress: usize,
    pub failed: usize,
}

#[derive(Debug, Clone)]
pub enum PeekedMessage {
    Envelope(CommandEnvelope),
    /// An entry that does not parse. `raw` holds its text, with invalid UTF-8 replaced.
    Malformed {
        raw: String,
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
    pub async fn open(lane: QueueLane, settings: &QueueSettings) -> Result<Queue, QueueError> {
        let backend = match settings.queue_driver {
            QueueDriver::File => Backend::File(FileDriver::open(
                &settings.queue_name,
                lane,
                &settings.file.base_path,
            )?),
            QueueDriver::Redis => Backend::Redis(
                RedisDriver::open(&settings.queue_name, lane, &settings.redis).await?,
            ),
        };
        Ok(Queue { backend })
    }

    pub fn lane(&self) -> QueueLane {
        match &self.backend {
            Backend::File(driver) => driver.lane(),
            Backend::Redis(driver) => driver.lane(),
        }
    }

    pub async fn publish(&self, envelope: &CommandEnvelope) -> Result<(), QueueError> {
        match &self.backend {
            Backend::File(driver) => driver.publish(envelope),
            Backend::Redis(driver) => driver.publish(envelope).await,
        }
    }

    /// An element that does not parse is moved to the failed stage and skipped.
    /// This future is not cancel safe. If it is dropped, the driver can still claim a message
    /// that no caller receives, so let it finish. It finishes within `wait`.
    pub async fn dequeue(&self, wait: Duration) -> Result<Option<Delivery>, QueueError> {
        match &self.backend {
            Backend::File(driver) => driver.dequeue(wait).await,
            Backend::Redis(driver) => driver.dequeue(wait).await,
        }
    }

    pub async fn acknowledge(&self, delivery: Delivery) -> Result<(), SettleError> {
        let settled = match (&self.backend, delivery.receipt()) {
            (Backend::File(driver), Receipt::File(name)) if self.owns(&delivery) => {
                driver.acknowledge(name)
            }
            (Backend::Redis(driver), Receipt::Redis(element)) if self.owns(&delivery) => {
                driver.acknowledge(element).await
            }
            _ => return Err(rejected(delivery)),
        };
        finish_settle(delivery, settled)
    }

    pub async fn retry(&self, delivery: Delivery) -> Result<(), SettleError> {
        self.move_claimed(delivery, Stage::Pending).await
    }

    pub async fn fail(&self, delivery: Delivery) -> Result<(), SettleError> {
        self.move_claimed(delivery, Stage::Failed).await
    }

    /// Stop every other consumer of the queue first. Recovery cannot tell a message left by a
    /// crash from one that another consumer is still processing, so that message would run
    /// twice.
    pub async fn recover(&self) -> Result<usize, QueueError> {
        match &self.backend {
            Backend::File(driver) => driver.recover(),
            Backend::Redis(driver) => driver.recover().await,
        }
    }

    pub async fn lengths(&self) -> Result<QueueLengths, QueueError> {
        match &self.backend {
            Backend::File(driver) => driver.lengths(),
            Backend::Redis(driver) => driver.lengths().await,
        }
    }

    pub async fn clear(&self, stage: Stage) -> Result<usize, QueueError> {
        match &self.backend {
            Backend::File(driver) => driver.clear(stage),
            Backend::Redis(driver) => driver.clear(stage).await,
        }
    }

    /// Position 0 of the pending stage is the next message to be claimed. A reversed range
    /// reads nothing.
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
            Backend::File(driver) => driver.peek(stage, first, last),
            Backend::Redis(driver) => driver.peek(stage, first, last).await,
        }
    }

    pub async fn republish_failed(&self, limit: usize) -> Result<usize, QueueError> {
        match &self.backend {
            Backend::File(driver) => driver.republish_failed(limit),
            Backend::Redis(driver) => driver.republish_failed(limit).await,
        }
    }

    async fn move_claimed(&self, delivery: Delivery, target: Stage) -> Result<(), SettleError> {
        let settled = match (&self.backend, delivery.receipt()) {
            (Backend::File(driver), Receipt::File(name)) if self.owns(&delivery) => {
                driver.move_claimed(name, &delivery.envelope, target)
            }
            (Backend::Redis(driver), Receipt::Redis(element)) if self.owns(&delivery) => {
                driver
                    .move_claimed(element, &delivery.envelope, target)
                    .await
            }
            _ => return Err(rejected(delivery)),
        };
        finish_settle(delivery, settled)
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
fn finish_settle(delivery: Delivery, settled: Result<bool, QueueError>) -> Result<(), SettleError> {
    match settled {
        Ok(true) => Ok(()),
        Ok(false) => Err(SettleError::Lost),
        Err(source) => Err(SettleError::Interrupted {
            delivery: Box::new(delivery),
            source,
        }),
    }
}
