use std::sync::Arc;

use tokio_util::sync::CancellationToken;
use tracing::info;

use crate::config::Settings;
use crate::queue::registry::CommandRegistry;
use crate::scheduling::scheduler::Scheduler;
use crate::server::consumers::QueueConsumers;

pub struct Server {
    pub settings: Arc<Settings>,
    pub registry: Arc<CommandRegistry>,
}

impl Server {
    pub fn new(settings: Arc<Settings>, registry: Arc<CommandRegistry>) -> Self {
        Self { settings, registry }
    }

    pub async fn run(&self, cancel: CancellationToken) {
        info!("starting server");

        let scheduler = Scheduler::new(self.settings.clone());

        let consumers = QueueConsumers::new(self.settings.clone(), self.registry.clone());

        let scheduler_cancel = cancel.clone();
        let consumers_cancel = cancel.clone();

        tokio::join!(
            scheduler.run(scheduler_cancel),
            consumers.run(consumers_cancel),
        );

        info!("server stopped");
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn settings_in(dir: &tempfile::TempDir) -> Arc<Settings> {
        Arc::new(crate::config::settings_with_queue_base(dir.path()))
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn server_starts_and_stops_cleanly() {
        // Arrange
        let dir = tempfile::tempdir().expect("temporary directory");
        let registry = Arc::new(CommandRegistry::new());
        let server = Server::new(settings_in(&dir), registry);
        let cancel = CancellationToken::new();
        let cancel_clone = cancel.clone();

        // Act
        let handle = tokio::spawn(async move {
            server.run(cancel_clone).await;
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
        let dir = tempfile::tempdir().expect("temporary directory");
        let registry = Arc::new(CommandRegistry::new());
        let server = Server::new(settings_in(&dir), registry);
        let cancel = CancellationToken::new();

        // Act
        cancel.cancel();
        server.run(cancel).await;

        // Assert (reaching here without panic is the test)
    }
}
