use std::sync::Arc;

use chrono::Utc;
use tokio_util::sync::CancellationToken;
use tracing::{debug, error, info};

use crate::command::Command;
use crate::config::Settings;
use crate::error::CommandError;
use crate::scheduling::schedule::{Schedule, ScheduledJob};
use crate::server::lifecycle::run_with_lifecycle;

pub struct Scheduler {
    settings: Arc<Settings>,
    schedule: Schedule,
}

impl Scheduler {
    pub fn new(settings: Arc<Settings>, schedule: Schedule) -> Self {
        Self { settings, schedule }
    }

    /// Runs every job until `cancel` fires. Each job moves into its own task with its whole
    /// lifecycle, including its hooks.
    pub async fn run(self, cancel: CancellationToken) {
        let handles: Vec<_> = self
            .schedule
            .into_jobs()
            .into_iter()
            .map(|job| tokio::spawn(run_job(job, self.settings.clone(), cancel.clone())))
            .collect();
        for handle in handles {
            if let Err(error) = handle.await {
                error!("a scheduled job task ended abnormally: {error}");
            }
        }
    }
}

async fn run_job(job: ScheduledJob, settings: Arc<Settings>, cancel: CancellationToken) {
    let name = &job.name;
    loop {
        let Some(next_time) = job.cron_schedule.upcoming(Utc).next() else {
            debug!("no upcoming fire time for {name}, stopping");
            return;
        };
        let duration = (next_time - Utc::now()).to_std().unwrap_or_default();

        tokio::select! {
            _ = cancel.cancelled() => {
                debug!("scheduled job {name} cancelled");
                return;
            }
            _ = tokio::time::sleep(duration) => {}
        }

        info!("running scheduled command: {name}");
        let command: Arc<dyn Command> = job.command.clone();
        match run_with_lifecycle(command, settings.clone(), &job.lifecycle, &cancel).await {
            Ok(()) => debug!("scheduled command {name} completed"),
            Err(CommandError::Interrupted) => {
                info!("scheduled command {name} stopped because the server is stopping");
            }
            Err(e) => error!("scheduled command {name} failed: {e}"),
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::command::{Command, CommandContext, CommandResult};
    use crate::commands::test_helpers::make_ctx;
    use crate::error::CommandError;
    use std::sync::atomic::{AtomicUsize, Ordering};

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

    struct FailingCommand;

    impl Command for FailingCommand {
        fn name(&self) -> &str {
            "failing"
        }

        fn run(&self, _ctx: &mut CommandContext) -> CommandResult {
            Err(CommandError::failed("intentional"))
        }
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn scheduler_runs_command_at_least_once() {
        // Arrange
        let ctx = make_ctx();
        let count = Arc::new(AtomicUsize::new(0));
        let mut schedule = Schedule::new();
        let job = schedule
            .command(CountingCommand {
                count: count.clone(),
            })
            .every_second()
            .unwrap();
        schedule.add(job);
        let scheduler = Scheduler::new(ctx.settings.clone(), schedule);
        let cancel = CancellationToken::new();
        let cancel_clone = cancel.clone();

        // Act
        let handle = tokio::spawn(async move {
            scheduler.run(cancel_clone).await;
        });
        tokio::time::sleep(std::time::Duration::from_secs(2)).await;
        cancel.cancel();
        let _ = handle.await;

        // Assert
        assert!(count.load(Ordering::SeqCst) >= 1);
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn failing_command_does_not_stop_scheduler() {
        // Arrange
        let ctx = make_ctx();
        let count = Arc::new(AtomicUsize::new(0));
        let mut schedule = Schedule::new();
        let failing_job = schedule.command(FailingCommand).every_second().unwrap();
        schedule.add(failing_job);
        let counting_job = schedule
            .command(CountingCommand {
                count: count.clone(),
            })
            .every_second()
            .unwrap();
        schedule.add(counting_job);
        let scheduler = Scheduler::new(ctx.settings.clone(), schedule);

        let cancel = CancellationToken::new();
        let cancel_clone = cancel.clone();

        // Act
        let handle = tokio::spawn(async move {
            scheduler.run(cancel_clone).await;
        });
        tokio::time::sleep(std::time::Duration::from_secs(2)).await;
        cancel.cancel();
        let _ = handle.await;

        // Assert
        assert!(count.load(Ordering::SeqCst) >= 1);
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn scheduled_job_runs_its_before_and_after_hooks() {
        // Arrange
        let ctx = make_ctx();
        let before = Arc::new(AtomicUsize::new(0));
        let after = Arc::new(AtomicUsize::new(0));
        let mut schedule = Schedule::new();
        let (before_hook, after_hook) = (before.clone(), after.clone());
        let job = schedule
            .command(CountingCommand {
                count: Arc::new(AtomicUsize::new(0)),
            })
            .every_second()
            .unwrap()
            .before_run(move || {
                before_hook.fetch_add(1, Ordering::SeqCst);
            })
            .after_run(move || {
                after_hook.fetch_add(1, Ordering::SeqCst);
            });
        schedule.add(job);
        let scheduler = Scheduler::new(ctx.settings.clone(), schedule);
        let cancel = CancellationToken::new();
        let cancel_clone = cancel.clone();
        let handle = tokio::spawn(async move { scheduler.run(cancel_clone).await });

        // Act
        tokio::time::sleep(std::time::Duration::from_millis(2500)).await;

        // Assert
        cancel.cancel();
        let _ = handle.await;
        assert!(before.load(Ordering::SeqCst) >= 1);
        assert!(after.load(Ordering::SeqCst) >= 1);
    }
}
