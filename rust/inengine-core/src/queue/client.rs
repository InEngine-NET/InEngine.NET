use crate::config::{QueueDriver, QueueSettings};
use crate::error::QueueError;
use crate::queue::envelope::CommandEnvelope;
use crate::queue::file_client::FileClient;
use crate::queue::redis_client::RedisClient;

pub struct QueueLengths {
    pub pending: u64,
    pub in_progress: u64,
    pub failed: u64,
}

pub trait QueueClient: Send + Sync {
    fn queue_base_name(&self) -> &str;
    fn queue_name(&self) -> &str;
    fn use_compression(&self) -> bool;

    fn publish(
        &self,
        envelope: &CommandEnvelope,
    ) -> impl std::future::Future<Output = Result<(), QueueError>> + Send;

    fn dequeue(
        &self,
    ) -> impl std::future::Future<Output = Result<Option<CommandEnvelope>, QueueError>> + Send;

    fn recover(&self) -> impl std::future::Future<Output = Result<(), QueueError>> + Send;

    fn queue_lengths(
        &self,
    ) -> impl std::future::Future<Output = Result<QueueLengths, QueueError>> + Send;

    fn clear_pending(
        &self,
    ) -> impl std::future::Future<Output = Result<bool, QueueError>> + Send;

    fn clear_in_progress(
        &self,
    ) -> impl std::future::Future<Output = Result<bool, QueueError>> + Send;

    fn clear_failed(
        &self,
    ) -> impl std::future::Future<Output = Result<bool, QueueError>> + Send;

    fn republish_failed(
        &self,
        limit: usize,
    ) -> impl std::future::Future<Output = Result<usize, QueueError>> + Send;

    fn peek_pending(
        &self,
        from: usize,
        to: usize,
    ) -> impl std::future::Future<Output = Result<Vec<CommandEnvelope>, QueueError>> + Send;

    fn peek_in_progress(
        &self,
        from: usize,
        to: usize,
    ) -> impl std::future::Future<Output = Result<Vec<CommandEnvelope>, QueueError>> + Send;

    fn peek_failed(
        &self,
        from: usize,
        to: usize,
    ) -> impl std::future::Future<Output = Result<Vec<CommandEnvelope>, QueueError>> + Send;

    fn remove_in_progress(
        &self,
        envelope: &CommandEnvelope,
    ) -> impl std::future::Future<Output = Result<(), QueueError>> + Send;

    fn move_to_failed(
        &self,
        envelope: &CommandEnvelope,
    ) -> impl std::future::Future<Output = Result<(), QueueError>> + Send;
}

pub fn make_queue_client(
    use_secondary: bool,
    settings: &QueueSettings,
) -> Result<Box<dyn QueueClientDyn>, QueueError> {
    let queue_name = if use_secondary {
        "Secondary"
    } else {
        "Primary"
    };

    match settings.queue_driver {
        QueueDriver::File => Ok(Box::new(FileClient::new(
            &settings.queue_name,
            queue_name,
            settings.use_compression,
            &settings.file.base_path,
        ))),
        QueueDriver::Redis => Ok(Box::new(RedisClient::new(
            &settings.queue_name,
            queue_name,
            settings.use_compression,
            &settings.redis,
        )?)),
    }
}

pub trait QueueClientDyn: Send + Sync {
    fn queue_base_name(&self) -> &str;
    fn queue_name(&self) -> &str;
    fn use_compression(&self) -> bool;

    fn publish_dyn<'a>(
        &'a self,
        envelope: &'a CommandEnvelope,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<(), QueueError>> + Send + 'a>>;

    fn dequeue_dyn(
        &self,
    ) -> std::pin::Pin<
        Box<dyn std::future::Future<Output = Result<Option<CommandEnvelope>, QueueError>> + Send + '_>,
    >;

    fn recover_dyn(
        &self,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<(), QueueError>> + Send + '_>>;

    fn queue_lengths_dyn(
        &self,
    ) -> std::pin::Pin<
        Box<dyn std::future::Future<Output = Result<QueueLengths, QueueError>> + Send + '_>,
    >;

    fn clear_pending_dyn(
        &self,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<bool, QueueError>> + Send + '_>>;

    fn clear_in_progress_dyn(
        &self,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<bool, QueueError>> + Send + '_>>;

    fn clear_failed_dyn(
        &self,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<bool, QueueError>> + Send + '_>>;

    fn republish_failed_dyn(
        &self,
        limit: usize,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<usize, QueueError>> + Send + '_>>;

    fn peek_pending_dyn(
        &self,
        from: usize,
        to: usize,
    ) -> std::pin::Pin<
        Box<dyn std::future::Future<Output = Result<Vec<CommandEnvelope>, QueueError>> + Send + '_>,
    >;

    fn peek_in_progress_dyn(
        &self,
        from: usize,
        to: usize,
    ) -> std::pin::Pin<
        Box<dyn std::future::Future<Output = Result<Vec<CommandEnvelope>, QueueError>> + Send + '_>,
    >;

    fn peek_failed_dyn(
        &self,
        from: usize,
        to: usize,
    ) -> std::pin::Pin<
        Box<dyn std::future::Future<Output = Result<Vec<CommandEnvelope>, QueueError>> + Send + '_>,
    >;

    fn remove_in_progress_dyn<'a>(
        &'a self,
        envelope: &'a CommandEnvelope,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<(), QueueError>> + Send + 'a>>;

    fn move_to_failed_dyn<'a>(
        &'a self,
        envelope: &'a CommandEnvelope,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<(), QueueError>> + Send + 'a>>;
}

impl<T: QueueClient> QueueClientDyn for T {
    fn queue_base_name(&self) -> &str {
        QueueClient::queue_base_name(self)
    }

    fn queue_name(&self) -> &str {
        QueueClient::queue_name(self)
    }

    fn use_compression(&self) -> bool {
        QueueClient::use_compression(self)
    }

    fn publish_dyn<'a>(
        &'a self,
        envelope: &'a CommandEnvelope,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<(), QueueError>> + Send + 'a>> {
        Box::pin(self.publish(envelope))
    }

    fn dequeue_dyn(
        &self,
    ) -> std::pin::Pin<
        Box<dyn std::future::Future<Output = Result<Option<CommandEnvelope>, QueueError>> + Send + '_>,
    > {
        Box::pin(self.dequeue())
    }

    fn recover_dyn(
        &self,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<(), QueueError>> + Send + '_>> {
        Box::pin(self.recover())
    }

    fn queue_lengths_dyn(
        &self,
    ) -> std::pin::Pin<
        Box<dyn std::future::Future<Output = Result<QueueLengths, QueueError>> + Send + '_>,
    > {
        Box::pin(self.queue_lengths())
    }

    fn clear_pending_dyn(
        &self,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<bool, QueueError>> + Send + '_>> {
        Box::pin(self.clear_pending())
    }

    fn clear_in_progress_dyn(
        &self,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<bool, QueueError>> + Send + '_>> {
        Box::pin(self.clear_in_progress())
    }

    fn clear_failed_dyn(
        &self,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<bool, QueueError>> + Send + '_>> {
        Box::pin(self.clear_failed())
    }

    fn republish_failed_dyn(
        &self,
        limit: usize,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<usize, QueueError>> + Send + '_>> {
        Box::pin(self.republish_failed(limit))
    }

    fn peek_pending_dyn(
        &self,
        from: usize,
        to: usize,
    ) -> std::pin::Pin<
        Box<dyn std::future::Future<Output = Result<Vec<CommandEnvelope>, QueueError>> + Send + '_>,
    > {
        Box::pin(self.peek_pending(from, to))
    }

    fn peek_in_progress_dyn(
        &self,
        from: usize,
        to: usize,
    ) -> std::pin::Pin<
        Box<dyn std::future::Future<Output = Result<Vec<CommandEnvelope>, QueueError>> + Send + '_>,
    > {
        Box::pin(self.peek_in_progress(from, to))
    }

    fn peek_failed_dyn(
        &self,
        from: usize,
        to: usize,
    ) -> std::pin::Pin<
        Box<dyn std::future::Future<Output = Result<Vec<CommandEnvelope>, QueueError>> + Send + '_>,
    > {
        Box::pin(self.peek_failed(from, to))
    }

    fn remove_in_progress_dyn<'a>(
        &'a self,
        envelope: &'a CommandEnvelope,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<(), QueueError>> + Send + 'a>> {
        Box::pin(self.remove_in_progress(envelope))
    }

    fn move_to_failed_dyn<'a>(
        &'a self,
        envelope: &'a CommandEnvelope,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<(), QueueError>> + Send + 'a>> {
        Box::pin(self.move_to_failed(envelope))
    }
}
