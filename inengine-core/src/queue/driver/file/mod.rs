mod dir;
mod store;

use std::ffi::OsStr;
use std::io;
use std::path::Path;
use std::sync::Arc;
use std::time::{Duration, Instant};

use crate::error::QueueError;
use crate::queue::CommandEnvelope;
use crate::queue::delivery::Delivery;
use crate::queue::{PeekedMessage, QueueLane, QueueLengths, Stage};
use store::FileStore;

const POLL_INTERVAL: Duration = Duration::from_secs(1);

/// The file queue. Each call runs its file operations on the blocking thread pool, so no file
/// I/O runs on an async worker thread.
pub(crate) struct FileDriver {
    lane: QueueLane,
    store: Arc<FileStore>,
}

/// Runs one store call on the blocking thread pool.
async fn call_store<T, F>(store: &Arc<FileStore>, call: F) -> Result<T, QueueError>
where
    F: FnOnce(&FileStore) -> Result<T, QueueError> + Send + 'static,
    T: Send + 'static,
{
    let store = Arc::clone(store);
    on_blocking_pool(move || call(&store)).await
}

/// Runs `task` on the blocking thread pool, and turns a panic into an IO error.
async fn on_blocking_pool<T, F>(task: F) -> Result<T, QueueError>
where
    F: FnOnce() -> Result<T, QueueError> + Send + 'static,
    T: Send + 'static,
{
    tokio::task::spawn_blocking(task)
        .await
        .map_err(|join_error| QueueError::Io(io::Error::other(join_error)))?
}

impl FileDriver {
    pub(crate) async fn open(
        queue_name: &str,
        lane: QueueLane,
        base_path: &Path,
    ) -> Result<Self, QueueError> {
        let queue_name = queue_name.to_string();
        let base_path = base_path.to_path_buf();
        let store =
            on_blocking_pool(move || FileStore::open(&queue_name, lane, &base_path)).await?;
        Ok(Self {
            lane,
            store: Arc::new(store),
        })
    }

    pub(crate) fn lane(&self) -> QueueLane {
        self.lane
    }

    pub(crate) async fn publish(&self, envelope: &CommandEnvelope) -> Result<(), QueueError> {
        let envelope = envelope.clone();
        call_store(&self.store, move |store| store.publish(&envelope)).await
    }

    pub(crate) async fn dequeue(&self, wait: Duration) -> Result<Option<Delivery>, QueueError> {
        let deadline = Instant::now().checked_add(wait);
        loop {
            if let Some(delivery) = call_store(&self.store, FileStore::claim_oldest).await? {
                return Ok(Some(delivery));
            }
            let remaining = match deadline {
                Some(deadline) => deadline.saturating_duration_since(Instant::now()),
                None => POLL_INTERVAL,
            };
            if remaining.is_zero() {
                return Ok(None);
            }
            tokio::time::sleep(remaining.min(POLL_INTERVAL)).await;
        }
    }

    /// Returns false when the message is no longer in progress.
    pub(crate) async fn acknowledge(&self, name: &OsStr) -> Result<bool, QueueError> {
        let name = name.to_os_string();
        call_store(&self.store, move |store| store.acknowledge(&name)).await
    }

    /// Returns false when the message is no longer in progress.
    pub(crate) async fn move_claimed(
        &self,
        name: &OsStr,
        envelope: &CommandEnvelope,
        target: Stage,
    ) -> Result<bool, QueueError> {
        let name = name.to_os_string();
        let envelope = envelope.clone();
        call_store(&self.store, move |store| {
            store.move_claimed(&name, &envelope, target)
        })
        .await
    }

    pub(crate) async fn recover(&self) -> Result<usize, QueueError> {
        call_store(&self.store, FileStore::recover).await
    }

    pub(crate) async fn lengths(&self) -> Result<QueueLengths, QueueError> {
        call_store(&self.store, FileStore::lengths).await
    }

    pub(crate) async fn clear(&self, stage: Stage) -> Result<usize, QueueError> {
        call_store(&self.store, move |store| store.clear(stage)).await
    }

    pub(crate) async fn peek(
        &self,
        stage: Stage,
        first: usize,
        last: usize,
    ) -> Result<Vec<PeekedMessage>, QueueError> {
        call_store(&self.store, move |store| store.peek(stage, first, last)).await
    }

    pub(crate) async fn republish_failed(&self, limit: usize) -> Result<usize, QueueError> {
        call_store(&self.store, move |store| store.republish_failed(limit)).await
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::error::ErrorChain;

    async fn open_store() -> (tempfile::TempDir, FileDriver) {
        let dir = tempfile::tempdir().expect("temporary directory");
        let driver = FileDriver::open("TestQueue", QueueLane::Primary, dir.path())
            .await
            .expect("open");
        (dir, driver)
    }

    #[tokio::test]
    async fn a_store_call_runs_off_the_calling_thread() {
        // Arrange
        let (_dir, driver) = open_store().await;
        let caller = std::thread::current().id();

        // Act
        let store_thread = call_store(&driver.store, |_store| Ok(std::thread::current().id()))
            .await
            .expect("the call succeeds");

        // Assert
        assert_ne!(store_thread, caller);
    }

    #[tokio::test]
    async fn a_panicking_store_call_becomes_an_io_error() {
        // Arrange
        let (_dir, driver) = open_store().await;

        // Act
        let result: Result<(), QueueError> =
            call_store(&driver.store, |_store| panic!("intentional panic")).await;

        // Assert
        let error = result.expect_err("the call panics");
        assert!(matches!(error, QueueError::Io(_)), "{error:?}");
        let message = ErrorChain::new(&error).to_string();
        assert!(
            message.starts_with("could not read or write the queue files: task "),
            "{message}"
        );
        assert!(message.contains("intentional panic"), "{message}");
    }
}
