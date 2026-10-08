use redis::AsyncCommands;

use crate::config::RedisSettings;
use crate::error::QueueError;
use crate::queue::client::{QueueClient, QueueLengths};
use crate::queue::envelope::CommandEnvelope;

pub struct RedisClient {
    queue_base_name: String,
    queue_name: String,
    use_compression: bool,
    pending_key: String,
    in_progress_key: String,
    failed_key: String,
    channel_name: String,
    client: redis::Client,
}

impl RedisClient {
    pub fn new(
        queue_base_name: &str,
        queue_name: &str,
        use_compression: bool,
        settings: &RedisSettings,
    ) -> Result<Self, QueueError> {
        let url = if settings.password.is_empty() {
            format!("redis://{}:{}/{}", settings.host, settings.port, settings.database)
        } else {
            format!(
                "redis://:{}@{}:{}/{}",
                settings.password, settings.host, settings.port, settings.database
            )
        };
        let client = redis::Client::open(url)?;
        Ok(Self {
            pending_key: format!("{queue_base_name}:{queue_name}:Pending"),
            in_progress_key: format!("{queue_base_name}:{queue_name}:InProgress"),
            failed_key: format!("{queue_base_name}:{queue_name}:Failed"),
            channel_name: queue_base_name.to_string(),
            queue_base_name: queue_base_name.to_string(),
            queue_name: queue_name.to_string(),
            use_compression,
            client,
        })
    }

    async fn get_connection(
        &self,
    ) -> Result<redis::aio::MultiplexedConnection, QueueError> {
        Ok(self.client.get_multiplexed_async_connection().await?)
    }
}

impl QueueClient for RedisClient {
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
        let json = serde_json::to_string(envelope)?;
        let mut conn = self.get_connection().await?;
        conn.lpush::<_, _, ()>(&self.pending_key, &json).await?;
        conn.publish::<_, _, ()>(&self.channel_name, "published").await?;
        Ok(())
    }

    async fn dequeue(&self) -> Result<Option<CommandEnvelope>, QueueError> {
        let mut conn = self.get_connection().await?;
        let result: Option<String> = redis::cmd("LMOVE")
            .arg(&self.pending_key)
            .arg(&self.in_progress_key)
            .arg("RIGHT")
            .arg("LEFT")
            .query_async(&mut conn)
            .await?;

        match result {
            Some(json) => {
                let envelope: CommandEnvelope = serde_json::from_str(&json)?;
                Ok(Some(envelope))
            }
            None => Ok(None),
        }
    }

    async fn recover(&self) -> Result<(), QueueError> {
        let mut conn = self.get_connection().await?;
        let len: u64 = conn.llen(&self.pending_key).await?;
        for _ in 0..len {
            conn.publish::<_, _, ()>(&self.channel_name, "recover")
                .await?;
        }
        Ok(())
    }

    async fn queue_lengths(&self) -> Result<QueueLengths, QueueError> {
        let mut conn = self.get_connection().await?;
        let pending: u64 = conn.llen(&self.pending_key).await?;
        let in_progress: u64 = conn.llen(&self.in_progress_key).await?;
        let failed: u64 = conn.llen(&self.failed_key).await?;
        Ok(QueueLengths {
            pending,
            in_progress,
            failed,
        })
    }

    async fn clear_pending(&self) -> Result<bool, QueueError> {
        let mut conn = self.get_connection().await?;
        let deleted: bool = conn.del(&self.pending_key).await?;
        Ok(deleted)
    }

    async fn clear_in_progress(&self) -> Result<bool, QueueError> {
        let mut conn = self.get_connection().await?;
        let deleted: bool = conn.del(&self.in_progress_key).await?;
        Ok(deleted)
    }

    async fn clear_failed(&self) -> Result<bool, QueueError> {
        let mut conn = self.get_connection().await?;
        let deleted: bool = conn.del(&self.failed_key).await?;
        Ok(deleted)
    }

    async fn republish_failed(&self, limit: usize) -> Result<usize, QueueError> {
        let mut conn = self.get_connection().await?;
        let mut count = 0;
        for _ in 0..limit {
            let result: Option<String> = redis::cmd("LMOVE")
                .arg(&self.failed_key)
                .arg(&self.pending_key)
                .arg("RIGHT")
                .arg("LEFT")
                .query_async(&mut conn)
                .await?;
            if result.is_none() {
                break;
            }
            count += 1;
        }
        Ok(count)
    }

    async fn peek_pending(
        &self,
        from: usize,
        to: usize,
    ) -> Result<Vec<CommandEnvelope>, QueueError> {
        self.peek_list(&self.pending_key, from, to).await
    }

    async fn peek_in_progress(
        &self,
        from: usize,
        to: usize,
    ) -> Result<Vec<CommandEnvelope>, QueueError> {
        self.peek_list(&self.in_progress_key, from, to).await
    }

    async fn peek_failed(
        &self,
        from: usize,
        to: usize,
    ) -> Result<Vec<CommandEnvelope>, QueueError> {
        self.peek_list(&self.failed_key, from, to).await
    }

    async fn remove_in_progress(&self, envelope: &CommandEnvelope) -> Result<(), QueueError> {
        let mut conn = self.get_connection().await?;
        let json = serde_json::to_string(envelope)?;
        conn.lrem::<_, _, ()>(&self.in_progress_key, 1, &json).await?;
        Ok(())
    }

    async fn move_to_failed(&self, envelope: &CommandEnvelope) -> Result<(), QueueError> {
        let mut conn = self.get_connection().await?;
        let json = serde_json::to_string(envelope)?;
        conn.lrem::<_, _, ()>(&self.in_progress_key, 1, &json).await?;
        conn.lpush::<_, _, ()>(&self.failed_key, &json).await?;
        Ok(())
    }
}

impl RedisClient {
    async fn peek_list(
        &self,
        key: &str,
        from: usize,
        to: usize,
    ) -> Result<Vec<CommandEnvelope>, QueueError> {
        let mut conn = self.get_connection().await?;
        let items: Vec<String> = conn.lrange(key, from as isize, to as isize).await?;
        items
            .iter()
            .map(|json| {
                let envelope: CommandEnvelope = serde_json::from_str(json)?;
                Ok(envelope)
            })
            .collect()
    }
}
