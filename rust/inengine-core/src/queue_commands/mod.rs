pub mod management;
pub mod messaging;

use std::error::Error;
use std::future::Future;

use crate::config::QueueSettings;
use crate::error::CommandError;
use crate::queue::{Queue, QueueLane};

/// Needs a multi-thread runtime, because `block_in_place` panics on a current-thread one.
fn block_on<F: Future>(future: F) -> F::Output {
    tokio::task::block_in_place(|| tokio::runtime::Handle::current().block_on(future))
}

async fn open_queue(lane: QueueLane, settings: &QueueSettings) -> Result<Queue, CommandError> {
    Queue::open(lane, settings).await.map_err(|error| {
        command_failed(
            &format!("could not open the {} queue", lane.as_str()),
            error,
        )
    })
}

fn command_failed(context: &str, error: impl Error + Send + Sync + 'static) -> CommandError {
    CommandError::FailedWithSource {
        message: context.to_string(),
        source: Box::new(error),
    }
}
