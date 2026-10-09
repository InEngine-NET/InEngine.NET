use std::sync::Arc;

use tokio_util::sync::CancellationToken;
use tracing::info;

use crate::config::Settings;
use crate::queue::registry::CommandRegistry;
use crate::scheduling::schedule::Schedule;
use crate::scheduling::scheduler::Scheduler;
use crate::server::consumers::QueueConsumers;

pub struct Server {
    settings: Arc<Settings>,
    registry: Arc<CommandRegistry>,
    schedule: Schedule,
}

impl Server {
    /// Runs the jobs in `schedule` and the queued commands that `registry` can build.
    pub fn new(
        settings: Arc<Settings>,
        registry: Arc<CommandRegistry>,
        schedule: Schedule,
    ) -> Self {
        Self {
            settings,
            registry,
            schedule,
        }
    }

    /// Runs until `cancel` fires. Running plugin calls and exec processes stop at once, and
    /// the server waits for every other running command to finish.
    pub async fn run(self, cancel: CancellationToken) {
        info!("starting server");

        let scheduler = Scheduler::new(self.settings.clone(), self.schedule);
        let consumers = QueueConsumers::new(self.settings.clone(), self.registry.clone());

        tokio::join!(scheduler.run(cancel.clone()), consumers.run(cancel.clone()));

        info!("server stopped");
    }
}

#[cfg(test)]
mod tests {
    use std::sync::atomic::{AtomicUsize, Ordering};

    use super::*;
    use crate::command::{Command, CommandContext, CommandResult};

    struct CountingCommand {
        count: Arc<AtomicUsize>,
    }

    impl Command for CountingCommand {
        fn name(&self) -> &str {
            "counting"
        }

        fn run(&self, _ctx: &mut CommandContext) -> CommandResult {
            self.count.fetch_add(1, Ordering::SeqCst);
            Ok(())
        }
    }

    fn settings_in(dir: &tempfile::TempDir) -> Arc<Settings> {
        Arc::new(crate::config::settings_with_queue_base(dir.path()))
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn server_starts_and_stops_cleanly() {
        // Arrange
        let dir = tempfile::tempdir().expect("temporary directory");
        let registry = Arc::new(CommandRegistry::new());
        let server = Server::new(settings_in(&dir), registry, Schedule::new());
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
        let server = Server::new(settings_in(&dir), registry, Schedule::new());
        let cancel = CancellationToken::new();

        // Act
        cancel.cancel();
        server.run(cancel).await;

        // Assert (reaching here without panic is the test)
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn server_runs_a_job_from_its_schedule() {
        // Arrange
        let dir = tempfile::tempdir().expect("temporary directory");
        let ran = Arc::new(AtomicUsize::new(0));
        let mut schedule = Schedule::new();
        let job = schedule
            .command(CountingCommand { count: ran.clone() })
            .every_second()
            .unwrap();
        schedule.add(job);
        let server = Server::new(
            settings_in(&dir),
            Arc::new(CommandRegistry::new()),
            schedule,
        );
        let cancel = CancellationToken::new();
        let handle = tokio::spawn(server.run(cancel.clone()));

        // Act
        tokio::time::sleep(std::time::Duration::from_secs(3)).await;

        // Assert
        cancel.cancel();
        handle.await.expect("the server stops");
        assert!(ran.load(Ordering::SeqCst) >= 1);
    }
}
