use std::path::{Path, PathBuf};

use tokio::sync::Mutex;

use crate::error::QueueError;
use crate::queue::client::{QueueClient, QueueLengths};
use crate::queue::envelope::CommandEnvelope;

pub struct FileClient {
    queue_base_name: String,
    queue_name: String,
    use_compression: bool,
    pending_path: PathBuf,
    in_progress_path: PathBuf,
    failed_path: PathBuf,
    consume_lock: Mutex<()>,
}

impl FileClient {
    pub fn new(
        queue_base_name: &str,
        queue_name: &str,
        use_compression: bool,
        base_path: &str,
    ) -> Self {
        let prefix = format!("{}_{}", queue_base_name, queue_name);
        let base = Path::new(base_path);
        Self {
            queue_base_name: queue_base_name.to_string(),
            queue_name: queue_name.to_string(),
            use_compression,
            pending_path: base.join(format!("{}_Pending", prefix)),
            in_progress_path: base.join(format!("{}_InProgress", prefix)),
            failed_path: base.join(format!("{}_Failed", prefix)),
            consume_lock: Mutex::new(()),
        }
    }

    fn ensure_dir(path: &Path) -> Result<(), QueueError> {
        if !path.exists() {
            std::fs::create_dir_all(path)?;
        }
        Ok(())
    }

    fn list_files_by_age(dir: &Path) -> Result<Vec<PathBuf>, QueueError> {
        Self::ensure_dir(dir)?;
        let mut entries: Vec<_> = std::fs::read_dir(dir)?
            .filter_map(|e| e.ok())
            .filter(|e| e.path().is_file())
            .collect();
        entries.sort_by_key(|e| {
            e.metadata()
                .and_then(|m| m.modified())
                .unwrap_or(std::time::SystemTime::UNIX_EPOCH)
        });
        Ok(entries.into_iter().map(|e| e.path()).collect())
    }

    fn read_envelopes(
        dir: &Path,
        from: usize,
        to: usize,
    ) -> Result<Vec<CommandEnvelope>, QueueError> {
        let files = Self::list_files_by_age(dir)?;
        let end = (to + 1).min(files.len());
        let start = from.min(end);
        files[start..end]
            .iter()
            .map(|path| {
                let content = std::fs::read_to_string(path)?;
                let envelope: CommandEnvelope = serde_json::from_str(&content)?;
                Ok(envelope)
            })
            .collect()
    }

    fn clear_dir(dir: &Path) -> Result<bool, QueueError> {
        Self::ensure_dir(dir)?;
        let files = Self::list_files_by_age(dir)?;
        let had_files = !files.is_empty();
        for path in files {
            std::fs::remove_file(path)?;
        }
        Ok(had_files)
    }
}

impl QueueClient for FileClient {
    fn queue_base_name(&self) -> &str {
        &self.queue_base_name
    }

    fn queue_name(&self) -> &str {
        &self.queue_name
    }

    fn use_compression(&self) -> bool {
        self.use_compression
    }

    async fn publish(&self, envelope: &CommandEnvelope) -> Result<(), QueueError> {
        Self::ensure_dir(&self.pending_path)?;
        let file_path = self.pending_path.join(&envelope.id);
        let json = serde_json::to_string_pretty(envelope)?;
        std::fs::write(file_path, json)?;
        Ok(())
    }

    async fn dequeue(&self) -> Result<Option<CommandEnvelope>, QueueError> {
        let guard = self.consume_lock.lock().await;
        Self::ensure_dir(&self.pending_path)?;
        Self::ensure_dir(&self.in_progress_path)?;

        let files = Self::list_files_by_age(&self.pending_path)?;
        let oldest = match files.first() {
            Some(path) => path,
            None => return Ok(None),
        };

        let file_name = oldest
            .file_name()
            .ok_or_else(|| QueueError::Other {
                message: "could not get file name".to_string(),
            })?
            .to_string_lossy()
            .to_string();

        let dest = self.in_progress_path.join(&file_name);
        std::fs::rename(oldest, &dest)?;
        drop(guard);

        let content = std::fs::read_to_string(&dest)?;
        let envelope: CommandEnvelope = serde_json::from_str(&content)?;
        Ok(Some(envelope))
    }

    async fn recover(&self) -> Result<(), QueueError> {
        Ok(())
    }

    async fn queue_lengths(&self) -> Result<QueueLengths, QueueError> {
        Self::ensure_dir(&self.pending_path)?;
        Self::ensure_dir(&self.in_progress_path)?;
        Self::ensure_dir(&self.failed_path)?;
        Ok(QueueLengths {
            pending: Self::list_files_by_age(&self.pending_path)?.len() as u64,
            in_progress: Self::list_files_by_age(&self.in_progress_path)?.len() as u64,
            failed: Self::list_files_by_age(&self.failed_path)?.len() as u64,
        })
    }

    async fn clear_pending(&self) -> Result<bool, QueueError> {
        Self::clear_dir(&self.pending_path)
    }

    async fn clear_in_progress(&self) -> Result<bool, QueueError> {
        Self::clear_dir(&self.in_progress_path)
    }

    async fn clear_failed(&self) -> Result<bool, QueueError> {
        Self::clear_dir(&self.failed_path)
    }

    async fn republish_failed(&self, limit: usize) -> Result<usize, QueueError> {
        Self::ensure_dir(&self.failed_path)?;
        Self::ensure_dir(&self.pending_path)?;
        let files = Self::list_files_by_age(&self.failed_path)?;
        let count = limit.min(files.len());
        for path in &files[..count] {
            let file_name = path.file_name().unwrap().to_string_lossy().to_string();
            std::fs::rename(path, self.pending_path.join(file_name))?;
        }
        Ok(count)
    }

    async fn peek_pending(
        &self,
        from: usize,
        to: usize,
    ) -> Result<Vec<CommandEnvelope>, QueueError> {
        Self::read_envelopes(&self.pending_path, from, to)
    }

    async fn peek_in_progress(
        &self,
        from: usize,
        to: usize,
    ) -> Result<Vec<CommandEnvelope>, QueueError> {
        Self::read_envelopes(&self.in_progress_path, from, to)
    }

    async fn peek_failed(
        &self,
        from: usize,
        to: usize,
    ) -> Result<Vec<CommandEnvelope>, QueueError> {
        Self::read_envelopes(&self.failed_path, from, to)
    }

    async fn remove_in_progress(&self, envelope: &CommandEnvelope) -> Result<(), QueueError> {
        let path = self.in_progress_path.join(&envelope.id);
        if path.exists() {
            std::fs::remove_file(path)?;
        }
        Ok(())
    }

    async fn move_to_failed(&self, envelope: &CommandEnvelope) -> Result<(), QueueError> {
        Self::ensure_dir(&self.failed_path)?;
        let src = self.in_progress_path.join(&envelope.id);
        let dest = self.failed_path.join(&envelope.id);
        if src.exists() {
            std::fs::rename(src, dest)?;
        } else {
            let json = serde_json::to_string_pretty(envelope)?;
            std::fs::write(dest, json)?;
        }
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::commands::echo::Echo;
    use tempfile::TempDir;

    fn make_client(dir: &TempDir) -> FileClient {
        FileClient::new("TestQueue", "Primary", false, dir.path().to_str().unwrap())
    }

    fn make_envelope(id: &str) -> CommandEnvelope {
        CommandEnvelope::from_command(
            &Echo {
                text: format!("msg-{id}"),
            },
            false,
        )
        .unwrap()
    }

    #[tokio::test]
    async fn publish_then_dequeue() {
        // Arrange
        let dir = TempDir::new().unwrap();
        let client = make_client(&dir);
        let envelope = make_envelope("1");

        // Act
        client.publish(&envelope).await.unwrap();
        let dequeued = client.dequeue().await.unwrap();

        // Assert
        assert!(dequeued.is_some());
        let dequeued = dequeued.unwrap();
        assert_eq!(dequeued.id, envelope.id);
    }

    #[tokio::test]
    async fn dequeue_in_order() {
        // Arrange
        let dir = TempDir::new().unwrap();
        let client = make_client(&dir);
        let e1 = make_envelope("first");
        let e2 = make_envelope("second");
        client.publish(&e1).await.unwrap();
        std::thread::sleep(std::time::Duration::from_millis(50));
        client.publish(&e2).await.unwrap();

        // Act
        let d1 = client.dequeue().await.unwrap().unwrap();
        let d2 = client.dequeue().await.unwrap().unwrap();

        // Assert
        assert_eq!(d1.id, e1.id);
        assert_eq!(d2.id, e2.id);
    }

    #[tokio::test]
    async fn dequeue_empty_returns_none() {
        // Arrange
        let dir = TempDir::new().unwrap();
        let client = make_client(&dir);

        // Act
        let result = client.dequeue().await.unwrap();

        // Assert
        assert!(result.is_none());
    }

    #[tokio::test]
    async fn flush_pending() {
        // Arrange
        let dir = TempDir::new().unwrap();
        let client = make_client(&dir);
        client.publish(&make_envelope("1")).await.unwrap();
        client.publish(&make_envelope("2")).await.unwrap();

        // Act
        let cleared = client.clear_pending().await.unwrap();

        // Assert
        assert!(cleared);
        let lengths = client.queue_lengths().await.unwrap();
        assert_eq!(lengths.pending, 0);
    }

    #[tokio::test]
    async fn flush_in_progress() {
        // Arrange
        let dir = TempDir::new().unwrap();
        let client = make_client(&dir);
        client.publish(&make_envelope("1")).await.unwrap();
        client.dequeue().await.unwrap();

        // Act
        let cleared = client.clear_in_progress().await.unwrap();

        // Assert
        assert!(cleared);
        let lengths = client.queue_lengths().await.unwrap();
        assert_eq!(lengths.in_progress, 0);
    }

    #[tokio::test]
    async fn flush_failed() {
        // Arrange
        let dir = TempDir::new().unwrap();
        let client = make_client(&dir);
        FileClient::ensure_dir(&client.failed_path).unwrap();
        let envelope = make_envelope("1");
        let json = serde_json::to_string(&envelope).unwrap();
        std::fs::write(client.failed_path.join(&envelope.id), json).unwrap();

        // Act
        let cleared = client.clear_failed().await.unwrap();

        // Assert
        assert!(cleared);
        let lengths = client.queue_lengths().await.unwrap();
        assert_eq!(lengths.failed, 0);
    }

    #[tokio::test]
    async fn peek_pending_returns_envelopes() {
        // Arrange
        let dir = TempDir::new().unwrap();
        let client = make_client(&dir);
        client.publish(&make_envelope("a")).await.unwrap();
        client.publish(&make_envelope("b")).await.unwrap();

        // Act
        let peeked = client.peek_pending(0, 9).await.unwrap();

        // Assert
        assert_eq!(peeked.len(), 2);
    }

    #[tokio::test]
    async fn republish_failed_moves_to_pending() {
        // Arrange
        let dir = TempDir::new().unwrap();
        let client = make_client(&dir);
        FileClient::ensure_dir(&client.failed_path).unwrap();
        let envelope = make_envelope("1");
        let json = serde_json::to_string(&envelope).unwrap();
        std::fs::write(client.failed_path.join(&envelope.id), json).unwrap();

        // Act
        let count = client.republish_failed(100).await.unwrap();

        // Assert
        assert_eq!(count, 1);
        let lengths = client.queue_lengths().await.unwrap();
        assert_eq!(lengths.pending, 1);
        assert_eq!(lengths.failed, 0);
    }

    #[tokio::test]
    async fn queue_lengths_correct() {
        // Arrange
        let dir = TempDir::new().unwrap();
        let client = make_client(&dir);
        client.publish(&make_envelope("1")).await.unwrap();
        client.publish(&make_envelope("2")).await.unwrap();
        client.publish(&make_envelope("3")).await.unwrap();

        // Act
        let lengths = client.queue_lengths().await.unwrap();

        // Assert
        assert_eq!(lengths.pending, 3);
        assert_eq!(lengths.in_progress, 0);
        assert_eq!(lengths.failed, 0);
    }
}
