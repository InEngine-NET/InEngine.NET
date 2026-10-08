use std::sync::Arc;

use tokio_util::sync::CancellationToken;
use tracing::info;

use crate::config::Settings;
use crate::queue::dequeue::Dequeue;
use crate::queue::registry::CommandRegistry;
use crate::scheduling::scheduler::Scheduler;

pub struct ServerHost {
    pub settings: Arc<Settings>,
    pub registry: Arc<CommandRegistry>,
}

impl ServerHost {
    pub fn new(settings: Arc<Settings>, registry: Arc<CommandRegistry>) -> Self {
        Self { settings, registry }
    }

    pub async fn start(&self, cancel: CancellationToken) {
        info!("starting server");

        let scheduler = Scheduler::new(self.settings.clone());

        let dequeue = Dequeue {
            queue_settings: self.settings.queue.clone(),
            settings: self.settings.clone(),
            registry: self.registry.clone(),
        };

        let scheduler_cancel = cancel.clone();
        let dequeue_cancel = cancel.clone();

        tokio::join!(
            scheduler.start(scheduler_cancel),
            dequeue.start(dequeue_cancel),
        );

        info!("server stopped");
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::commands::test_helpers::make_ctx;

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn server_starts_and_stops_cleanly() {
        // Arrange
        let ctx = make_ctx();
        let registry = Arc::new(CommandRegistry::new());
        let host = ServerHost::new(ctx.settings.clone(), registry);
        let cancel = CancellationToken::new();
        let cancel_clone = cancel.clone();

        // Act
        let handle = tokio::spawn(async move {
            host.start(cancel_clone).await;
        });
        tokio::time::sleep(std::time::Duration::from_secs(1)).await;
        cancel.cancel();
        let result = handle.await;

        // Assert
        assert!(result.is_ok());
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn server_no_panic_with_empty_state() {
        // Arrange
        let ctx = make_ctx();
        let registry = Arc::new(CommandRegistry::new());
        let host = ServerHost::new(ctx.settings.clone(), registry);
        let cancel = CancellationToken::new();

        // Act
        cancel.cancel();
        host.start(cancel).await;

        // Assert (reaching here without panic is the test)
    }
}
