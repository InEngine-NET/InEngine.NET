use std::sync::Arc;

use chrono::Utc;
use tokio_util::sync::CancellationToken;
use tracing::{debug, error, info};

use crate::config::Settings;
use crate::scheduling::schedule::ScheduleBuilder;
use crate::server::lifecycle::{run_with_lifecycle, CommandLifecycle};

pub struct Scheduler {
    pub schedule: ScheduleBuilder,
}

impl Scheduler {
    pub fn new(settings: Arc<Settings>) -> Self {
        Self {
            schedule: ScheduleBuilder::new(settings),
        }
    }

    pub async fn start(&self, cancel: CancellationToken) {
        let mut handles = Vec::new();

        for reg in &self.schedule.registrations {
            let command = reg.command.clone();
            let command_name = reg.command_name.clone();
            let cron_schedule = reg.cron_schedule.clone();
            let settings = self.schedule.settings.clone();
            let token = cancel.clone();
            let lifecycle = CommandLifecycle {
                ping_before_url: reg.lifecycle.ping_before_url.clone(),
                ping_after_url: reg.lifecycle.ping_after_url.clone(),
                write_output_to: reg.lifecycle.write_output_to.clone(),
                append_output_to: reg.lifecycle.append_output_to.clone(),
                email_output_to: reg.lifecycle.email_output_to.clone(),
                timeout_seconds: reg.lifecycle.timeout_seconds,
                ..Default::default()
            };

            handles.push(tokio::spawn(async move {
                run_job(command, &command_name, cron_schedule, settings, lifecycle, token).await;
            }));
        }

        for handle in handles {
            let _ = handle.await;
        }
    }
}

async fn run_job(
    command: Arc<dyn crate::command::Command>,
    command_name: &str,
    cron_schedule: cron::Schedule,
    settings: Arc<Settings>,
    lifecycle: CommandLifecycle,
    cancel: CancellationToken,
) {
    loop {
        let next = cron_schedule.upcoming(Utc).next();
        let next_time = match next {
            Some(t) => t,
            None => {
                debug!("no upcoming fire time for {command_name}, stopping");
                return;
            }
        };

        let now = Utc::now();
        let duration = (next_time - now).to_std().unwrap_or_default();

        tokio::select! {
            _ = cancel.cancelled() => {
                debug!("scheduled job {command_name} cancelled");
                return;
            }
            _ = tokio::time::sleep(duration) => {}
        }

        if cancel.is_cancelled() {
            return;
        }

        info!("running scheduled command: {command_name}");

        match run_with_lifecycle(command.clone(), settings.clone(), &lifecycle).await {
            Ok(()) => debug!("scheduled command {command_name} completed"),
            Err(e) => error!("scheduled command {command_name} failed: {e}"),
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
            Err(CommandError::Failed {
                message: "intentional".to_string(),
            })
        }
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn scheduler_runs_command_at_least_once() {
        // Arrange
        let ctx = make_ctx();
        let count = Arc::new(AtomicUsize::new(0));
        let mut scheduler = Scheduler::new(ctx.settings.clone());
        let reg = scheduler
            .schedule
            .command(CountingCommand {
                count: count.clone(),
            })
            .every_second()
            .unwrap();
        scheduler.schedule.add(reg);
        let cancel = CancellationToken::new();
        let cancel_clone = cancel.clone();

        // Act
        let handle = tokio::spawn(async move {
            scheduler.start(cancel_clone).await;
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
        let mut scheduler = Scheduler::new(ctx.settings.clone());

        let reg_fail = scheduler
            .schedule
            .command(FailingCommand)
            .every_second()
            .unwrap();
        scheduler.schedule.add(reg_fail);

        let reg_count = scheduler
            .schedule
            .command(CountingCommand {
                count: count.clone(),
            })
            .every_second()
            .unwrap();
        scheduler.schedule.add(reg_count);

        let cancel = CancellationToken::new();
        let cancel_clone = cancel.clone();

        // Act
        let handle = tokio::spawn(async move {
            scheduler.start(cancel_clone).await;
        });
        tokio::time::sleep(std::time::Duration::from_secs(2)).await;
        cancel.cancel();
        let _ = handle.await;

        // Assert
        assert!(count.load(Ordering::SeqCst) >= 1);
    }
}
