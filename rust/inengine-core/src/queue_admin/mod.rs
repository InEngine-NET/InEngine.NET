//! Queue administration for the CLI. Each request runs as async code on the caller's runtime.

pub mod management;
pub mod messaging;

use crate::config::QueueSettings;
use crate::error::CommandError;
use crate::queue::{Queue, QueueLane};

async fn open_queue(lane: QueueLane, settings: &QueueSettings) -> Result<Queue, CommandError> {
    Queue::open(lane, settings).await.map_err(|error| {
        CommandError::failed_with(format!("could not open the {} queue", lane.as_str()), error)
    })
}
