use std::time::{Duration, Instant};

use redis::aio::{ConnectionManager, ConnectionManagerConfig};
use redis::{ConnectionAddr, ConnectionInfo, IntoConnectionInfo, RedisConnectionInfo, RedisResult};
use tracing::warn;

mod scripts;

use crate::config::RedisSettings;
use crate::error::{ErrorChain, QueueError};
use crate::queue::CommandEnvelope;
use crate::queue::delivery::{Delivery, Receipt};
use crate::queue::{PeekedMessage, QueueLane, QueueLengths, Stage};

const CONNECTION_TIMEOUT: Duration = Duration::from_secs(5);
const RESPONSE_TIMEOUT: Duration = Duration::from_secs(30);
/// Shorter than the response timeout, so that the reply arrives before the client stops
/// waiting. A later reply leaves the claimed element in progress until a recovery.
const MAX_BLOCKING_WAIT: Duration = RESPONSE_TIMEOUT.saturating_sub(Duration::from_secs(5));
const BULK_MOVE_BATCH_SIZE: usize = 1000;

/// Pending messages are pushed on the left and claimed from the right, so the right end
/// holds the oldest message.
pub(crate) struct RedisDriver {
    lane: QueueLane,
    pending_key: String,
    in_progress_key: String,
    failed_key: String,
    connection: ConnectionManager,
}

impl RedisDriver {
    pub(crate) async fn open(
        queue_name: &str,
        lane: QueueLane,
        settings: &RedisSettings,
    ) -> Result<Self, QueueError> {
        let client = redis::Client::open(connection_info(settings)?)?;
        let config = ConnectionManagerConfig::new()
            .set_connection_timeout(Some(CONNECTION_TIMEOUT))
            .set_response_timeout(Some(RESPONSE_TIMEOUT));
        let connection = ConnectionManager::new_with_config(client, config).await?;
        let stage_key = |stage: Stage| format!("{queue_name}:{}:{}", lane.as_str(), stage.as_str());
        Ok(Self {
            lane,
            pending_key: stage_key(Stage::Pending),
            in_progress_key: stage_key(Stage::InProgress),
            failed_key: stage_key(Stage::Failed),
            connection,
        })
    }

    pub(crate) fn lane(&self) -> QueueLane {
        self.lane
    }

    pub(crate) async fn publish(&self, envelope: &CommandEnvelope) -> Result<(), QueueError> {
        let json = serde_json::to_vec(envelope)?;
        redis::cmd("LPUSH")
            .arg(&self.pending_key)
            .arg(json)
            .query_async::<()>(&mut self.connection.clone())
            .await?;
        Ok(())
    }

    pub(crate) async fn dequeue(&self, wait: Duration) -> Result<Option<Delivery>, QueueError> {
        let deadline = Instant::now() + wait.min(MAX_BLOCKING_WAIT);
        loop {
            let remaining = deadline.saturating_duration_since(Instant::now());
            let Some(element) = self.claim_element(remaining).await? else {
                return Ok(None);
            };
            match serde_json::from_slice::<CommandEnvelope>(&element) {
                Ok(mut envelope) => {
                    envelope.attempts += 1;
                    return Ok(Some(Delivery::new(
                        envelope,
                        self.lane,
                        Receipt::Redis(element),
                    )));
                }
                Err(error) => self.move_unreadable_to_failed(&element, &error).await,
            }
        }
    }

    /// Returns false when the element is no longer in progress.
    pub(crate) async fn acknowledge(&self, element: &[u8]) -> Result<bool, QueueError> {
        let removed: i64 = redis::cmd("LREM")
            .arg(&self.in_progress_key)
            .arg(1)
            .arg(element)
            .query_async(&mut self.connection.clone())
            .await?;
        Ok(removed == 1)
    }

    /// Returns false when the element is no longer in progress.
    pub(crate) async fn move_claimed(
        &self,
        element: &[u8],
        envelope: &CommandEnvelope,
        target: Stage,
    ) -> Result<bool, QueueError> {
        let updated = serde_json::to_vec(envelope)?;
        self.move_if_present(element, self.stage_key(target), &updated)
            .await
    }

    pub(crate) async fn recover(&self) -> Result<usize, QueueError> {
        self.bulk_move(
            &self.in_progress_key,
            &self.pending_key,
            "LEFT",
            "RIGHT",
            usize::MAX,
        )
        .await
    }

    pub(crate) async fn lengths(&self) -> Result<QueueLengths, QueueError> {
        let (pending, in_progress, failed) = redis::pipe()
            .llen(&self.pending_key)
            .llen(&self.in_progress_key)
            .llen(&self.failed_key)
            .query_async::<(usize, usize, usize)>(&mut self.connection.clone())
            .await?;
        Ok(QueueLengths {
            pending,
            in_progress,
            failed,
        })
    }

    pub(crate) async fn clear(&self, stage: Stage) -> Result<usize, QueueError> {
        let key = self.stage_key(stage);
        let (length, _deleted) = redis::pipe()
            .atomic()
            .llen(key)
            .del(key)
            .query_async::<(usize, i64)>(&mut self.connection.clone())
            .await?;
        Ok(length)
    }

    /// Reads the elements at positions `first` to `last`, oldest first.
    pub(crate) async fn peek(
        &self,
        stage: Stage,
        first: usize,
        last: usize,
    ) -> Result<Vec<PeekedMessage>, QueueError> {
        let index_limit = isize::MAX as usize - 1;
        let first = first.min(index_limit) as isize;
        let last = last.min(index_limit) as isize;
        let elements: Vec<Vec<u8>> = redis::cmd("LRANGE")
            .arg(self.stage_key(stage))
            .arg(-(last + 1))
            .arg(-(first + 1))
            .query_async(&mut self.connection.clone())
            .await?;
        Ok(elements
            .iter()
            .rev()
            .map(|raw| PeekedMessage::parse(raw))
            .collect())
    }

    pub(crate) async fn republish_failed(&self, limit: usize) -> Result<usize, QueueError> {
        self.bulk_move(&self.failed_key, &self.pending_key, "RIGHT", "LEFT", limit)
            .await
    }

    async fn claim_element(&self, wait: Duration) -> Result<Option<Vec<u8>>, QueueError> {
        let command = if wait.is_zero() {
            redis::cmd("LMOVE")
        } else {
            redis::cmd("BLMOVE")
        };
        let mut command = command;
        command
            .arg(&self.pending_key)
            .arg(&self.in_progress_key)
            .arg("RIGHT")
            .arg("LEFT");
        if !wait.is_zero() {
            command.arg(wait.as_secs_f64());
        }
        Ok(command.query_async(&mut self.connection.clone()).await?)
    }

    async fn move_unreadable_to_failed(&self, element: &[u8], error: &serde_json::Error) {
        warn!(
            "moving unreadable message in {} to failed: {}",
            self.in_progress_key,
            ErrorChain::new(error)
        );
        if let Err(move_error) = self
            .move_if_present(element, &self.failed_key, element)
            .await
        {
            warn!(
                "could not move an unreadable message to failed: {}",
                ErrorChain::new(&move_error)
            );
        }
    }

    /// Redis runs one script at a time, so small batches keep other clients from waiting.
    async fn bulk_move(
        &self,
        source: &str,
        destination: &str,
        from: &str,
        to: &str,
        limit: usize,
    ) -> Result<usize, QueueError> {
        let mut moved = 0;
        while moved < limit {
            let batch = (limit - moved).min(BULK_MOVE_BATCH_SIZE);
            let count: usize = scripts::BULK_MOVE
                .key(source)
                .key(destination)
                .arg(batch)
                .arg(from)
                .arg(to)
                .invoke_async(&mut self.connection.clone())
                .await?;
            moved += count;
            if count < batch {
                break;
            }
        }
        Ok(moved)
    }

    async fn move_if_present(
        &self,
        element: &[u8],
        destination: &str,
        replacement: &[u8],
    ) -> Result<bool, QueueError> {
        let moved: i64 = scripts::MOVE_IF_PRESENT
            .key(&self.in_progress_key)
            .key(destination)
            .arg(element)
            .arg(replacement)
            .invoke_async(&mut self.connection.clone())
            .await?;
        Ok(moved == 1)
    }

    fn stage_key(&self, stage: Stage) -> &str {
        match stage {
            Stage::Pending => &self.pending_key,
            Stage::InProgress => &self.in_progress_key,
            Stage::Failed => &self.failed_key,
        }
    }
}

fn connection_info(settings: &RedisSettings) -> RedisResult<ConnectionInfo> {
    let mut redis = RedisConnectionInfo::default().set_db(i64::from(settings.database));
    if !settings.password.is_empty() {
        redis = redis.set_password(&settings.password);
    }
    Ok(ConnectionAddr::Tcp(settings.host.clone(), settings.port)
        .into_connection_info()?
        .set_redis_settings(redis))
}

#[cfg(test)]
mod tests {
    use super::*;

    fn settings(password: &str) -> RedisSettings {
        RedisSettings {
            host: "redis.internal".to_string(),
            port: 6380,
            database: 3,
            password: password.to_string(),
        }
    }

    #[test]
    fn connection_info_keeps_a_password_with_url_characters() {
        // Arrange
        let settings = settings("p@ss:word/1");

        // Act
        let info = connection_info(&settings).expect("connection info");

        // Assert
        assert_eq!(info.redis_settings().password(), Some("p@ss:word/1"));
        assert_eq!(info.redis_settings().db(), 3);
        assert!(matches!(info.addr(), ConnectionAddr::Tcp(host, 6380) if host == "redis.internal"));
    }

    #[test]
    fn connection_info_sets_no_password_when_it_is_empty() {
        // Arrange
        let settings = settings("");

        // Act
        let info = connection_info(&settings).expect("connection info");

        // Assert
        assert_eq!(info.redis_settings().password(), None);
    }
}
