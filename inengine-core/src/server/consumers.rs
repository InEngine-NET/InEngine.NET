use std::sync::Arc;
use std::time::Duration;

use tokio_util::sync::CancellationToken;
use tracing::{Instrument, debug, error, info, info_span, warn};
use uuid::Uuid;

use crate::config::{QueueSettings, Settings};
use crate::error::ErrorChain;
use crate::queue::{
    Attempt, AttemptKind, CommandRegistry, Delivery, Queue, QueueLane, SettleError, SettleOutcome,
    process_delivery, settle,
};
use crate::server::lifecycle::{CommandLifecycle, run_with_lifecycle};

const DEQUEUE_WAIT: Duration = Duration::from_secs(5);
const RETRY_DELAY: Duration = Duration::from_secs(5);
/// A settle after a shutdown cannot wait for the token, which has already fired, so it
/// retries a few times on a short delay before it leaves the message in progress.
const SHUTDOWN_SETTLE_RETRIES: u32 = 3;
const SHUTDOWN_SETTLE_DELAY: Duration = Duration::from_millis(500);

pub(crate) struct QueueConsumers {
    settings: Arc<Settings>,
    registry: Arc<CommandRegistry>,
    timing: ConsumerTiming,
}

#[derive(Debug, Clone, Copy)]
struct ConsumerTiming {
    dequeue_wait: Duration,
    retry_delay: Duration,
}

impl QueueConsumers {
    pub(crate) fn new(settings: Arc<Settings>, registry: Arc<CommandRegistry>) -> Self {
        Self {
            settings,
            registry,
            timing: ConsumerTiming {
                dequeue_wait: DEQUEUE_WAIT,
                retry_delay: RETRY_DELAY,
            },
        }
    }

    #[cfg(test)]
    fn with_timing(mut self, dequeue_wait: Duration, retry_delay: Duration) -> Self {
        self.timing = ConsumerTiming {
            dequeue_wait,
            retry_delay,
        };
        self
    }

    /// Runs the consumers until `cancel` fires and every running command has finished.
    pub(crate) async fn run(&self, cancel: CancellationToken) {
        let mut handles = Vec::new();
        for lane in QueueLane::ALL {
            let count = consumer_count(&self.settings.queue, lane);
            if count == 0 {
                continue;
            }
            recover_or_report_in_progress(lane, &self.settings.queue, self.timing, &cancel).await;
            for id in 0..count {
                let consumer = Consumer {
                    lane,
                    settings: self.settings.clone(),
                    registry: self.registry.clone(),
                    timing: self.timing,
                    cancel: cancel.clone(),
                };
                let span = info_span!("consumer", lane = lane.as_str(), id);
                handles.push(tokio::spawn(consumer.run().instrument(span)));
            }
        }
        for handle in handles {
            if let Err(error) = handle.await {
                error!(
                    "a queue consumer task ended abnormally: {}",
                    ErrorChain::new(&error)
                );
            }
        }
    }
}

fn consumer_count(settings: &QueueSettings, lane: QueueLane) -> usize {
    match lane {
        QueueLane::Primary => settings.primary_queue_consumers,
        QueueLane::Secondary => settings.secondary_queue_consumers,
    }
}

async fn recover_or_report_in_progress(
    lane: QueueLane,
    settings: &QueueSettings,
    timing: ConsumerTiming,
    cancel: &CancellationToken,
) {
    let Some(queue) = open_with_retry(lane, settings, timing, cancel).await else {
        return;
    };
    if settings.recover_on_start {
        match queue.recover().await {
            Ok(count) => info!(
                "in-progress messages returned to pending in the {} queue: {count}",
                lane.as_str()
            ),
            Err(error) => error!(
                "could not recover the {} queue: {}",
                lane.as_str(),
                ErrorChain::new(&error)
            ),
        }
        return;
    }
    match queue.lengths().await {
        Ok(lengths) if lengths.in_progress > 0 => warn!(
            "{} messages in the {} queue are in progress from an earlier run. Run \
             `inengine queue recover` or set queue.recover_on_start to process them.",
            lengths.in_progress,
            lane.as_str()
        ),
        Ok(_) => {}
        Err(error) => error!(
            "could not read the {} queue lengths: {}",
            lane.as_str(),
            ErrorChain::new(&error)
        ),
    }
}

async fn open_with_retry(
    lane: QueueLane,
    settings: &QueueSettings,
    timing: ConsumerTiming,
    cancel: &CancellationToken,
) -> Option<Queue> {
    loop {
        // Opening claims nothing, so it is safe to abandon. A Redis open can retry for many
        // seconds before it reports a failure.
        let opened = tokio::select! {
            _ = cancel.cancelled() => return None,
            opened = Queue::open(lane, settings) => opened,
        };
        match opened {
            Ok(queue) => return Some(queue),
            Err(error) => error!(
                "could not open the {} queue: {}",
                lane.as_str(),
                ErrorChain::new(&error)
            ),
        }
        if sleep_or_cancel(cancel, timing.retry_delay).await {
            return None;
        }
    }
}

/// Returns true when `cancel` fires before `duration` ends.
async fn sleep_or_cancel(cancel: &CancellationToken, duration: Duration) -> bool {
    tokio::select! {
        _ = cancel.cancelled() => true,
        _ = tokio::time::sleep(duration) => false,
    }
}

struct Consumer {
    lane: QueueLane,
    settings: Arc<Settings>,
    registry: Arc<CommandRegistry>,
    timing: ConsumerTiming,
    cancel: CancellationToken,
}

impl Consumer {
    async fn run(self) {
        let Some(queue) =
            open_with_retry(self.lane, &self.settings.queue, self.timing, &self.cancel).await
        else {
            return;
        };
        debug!("started");
        while !self.cancel.is_cancelled() {
            match queue.dequeue(self.timing.dequeue_wait).await {
                Ok(Some(delivery)) => self.process(&queue, delivery).await,
                Ok(None) => {}
                Err(error) => {
                    error!("could not dequeue: {}", ErrorChain::new(&error));
                    if sleep_or_cancel(&self.cancel, self.timing.retry_delay).await {
                        break;
                    }
                }
            }
        }
        debug!("stopped");
    }

    async fn process(&self, queue: &Queue, delivery: Delivery) {
        let settings = self.settings.clone();
        let cancel = self.cancel.clone();
        let processed = process_delivery(
            queue,
            delivery,
            &self.registry,
            &self.cancel,
            |command| async move {
                info!("running {}", command.name());
                let lifecycle = CommandLifecycle::default();
                run_with_lifecycle(Arc::from(command), settings, &lifecycle, &cancel).await
            },
        )
        .await;
        let id = processed.message_id;
        match &processed.attempt {
            Attempt::Succeeded => {}
            Attempt::CommandFailed(error) => {
                error!("message {id} failed: {}", ErrorChain::new(error))
            }
            Attempt::Unrunnable(error) => {
                error!(
                    "could not build the command for message {id}: {}",
                    ErrorChain::new(error)
                )
            }
            Attempt::Interrupted => info!("message {id} stopped because the server is stopping"),
        }
        self.settle_until_done(queue, id, processed.attempt.kind(), processed.settled)
            .await;
    }

    /// Logs the settle result. After an `Interrupted` error, settles again until the settle
    /// succeeds or the server stops. A message stopped by the shutdown gets a few more tries.
    async fn settle_until_done(
        &self,
        queue: &Queue,
        id: Uuid,
        attempt: AttemptKind,
        mut settled: Result<SettleOutcome, SettleError>,
    ) {
        let mut shutdown_retries = 0;
        loop {
            match settled {
                Ok(outcome) => return log_outcome(id, outcome),
                Err(SettleError::Lost) => {
                    return warn!("message {id} left the in-progress stage before it was settled");
                }
                Err(error @ SettleError::Rejected { .. }) => return error!("{error}"),
                Err(SettleError::Interrupted { delivery, source }) => {
                    error!(
                        "could not settle message {id}: {}",
                        ErrorChain::new(&source)
                    );
                    let give_up = if attempt == AttemptKind::Interrupted {
                        shutdown_retries += 1;
                        let exhausted = shutdown_retries > SHUTDOWN_SETTLE_RETRIES;
                        if !exhausted {
                            tokio::time::sleep(SHUTDOWN_SETTLE_DELAY).await;
                        }
                        exhausted
                    } else {
                        sleep_or_cancel(&self.cancel, self.timing.retry_delay).await
                    };
                    if give_up {
                        return warn!(
                            "message {id} stays in progress because the server is stopping. Run \
                             `inengine queue recover` after the server stops, or set \
                             queue.recover_on_start."
                        );
                    }
                    settled = settle(queue, *delivery, attempt).await;
                }
            }
        }
    }
}

fn log_outcome(id: Uuid, outcome: SettleOutcome) {
    match outcome {
        SettleOutcome::Acknowledged => debug!("completed message {id}"),
        SettleOutcome::Retried => info!("returned message {id} to pending for a retry"),
        SettleOutcome::MovedToFailed => warn!("moved message {id} to failed"),
        SettleOutcome::Released => {
            info!("returned message {id} to pending because the server is stopping")
        }
    }
}

#[cfg(test)]
mod tests {
    use std::path::Path;
    use std::time::Instant;

    use super::*;
    use crate::commands::echo::Echo;
    use crate::config::settings_with_queue_base;
    use crate::queue::{CommandEnvelope, QueueLengths};

    const SHORT_WAIT: Duration = Duration::from_millis(50);
    const LONG_DELAY: Duration = Duration::from_secs(30);

    fn file_settings(base: &Path, recover_on_start: bool) -> Settings {
        let mut settings = settings_with_queue_base(base);
        settings.queue.primary_queue_consumers = 1;
        settings.queue.secondary_queue_consumers = 0;
        settings.queue.recover_on_start = recover_on_start;
        settings
    }

    fn expected_lengths(pending: usize, in_progress: usize, failed: usize) -> QueueLengths {
        QueueLengths {
            pending,
            in_progress,
            failed,
        }
    }

    async fn leave_message_in_progress(settings: &Settings, lane: QueueLane, class: &str) -> Queue {
        let queue = Queue::open(lane, &settings.queue).await.expect("open");
        let json = serde_json::to_string(&Echo {
            text: "orphan".into(),
        })
        .expect("json");
        let envelope =
            CommandEnvelope::new(class, "inengine-core", &json, false, 0).expect("envelope");
        queue.publish(&envelope).await.expect("publish");
        let delivery = queue
            .dequeue(Duration::ZERO)
            .await
            .expect("dequeue")
            .expect("message");
        drop(delivery);
        queue
    }

    /// Runs the consumers until `cancel_after`, and returns how long they took to stop after
    /// the cancellation.
    async fn run_consumers_until(settings: Settings, cancel_after: Duration) -> Duration {
        let consumers = QueueConsumers::new(Arc::new(settings), Arc::new(CommandRegistry::new()))
            .with_timing(SHORT_WAIT, LONG_DELAY);
        let cancel = CancellationToken::new();
        let stopper = cancel.clone();
        let cancelled_at = tokio::spawn(async move {
            tokio::time::sleep(cancel_after).await;
            stopper.cancel();
            Instant::now()
        });
        consumers.run(cancel).await;
        let stopped_at = Instant::now();
        stopped_at.duration_since(cancelled_at.await.expect("stopper"))
    }

    const ECHO: &str = "echo";

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn recover_on_start_processes_a_message_left_in_progress() {
        // Arrange
        let dir = tempfile::tempdir().expect("temporary directory");
        let settings = file_settings(dir.path(), true);
        let queue = leave_message_in_progress(&settings, QueueLane::Primary, ECHO).await;

        // Act
        run_consumers_until(settings, Duration::from_millis(500)).await;

        // Assert
        assert_eq!(
            queue.lengths().await.expect("lengths"),
            expected_lengths(0, 0, 0)
        );
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn without_recover_on_start_a_message_stays_in_progress() {
        // Arrange
        let dir = tempfile::tempdir().expect("temporary directory");
        let settings = file_settings(dir.path(), false);
        let queue = leave_message_in_progress(&settings, QueueLane::Primary, ECHO).await;

        // Act
        run_consumers_until(settings, Duration::from_millis(500)).await;

        // Assert
        assert_eq!(
            queue.lengths().await.expect("lengths"),
            expected_lengths(0, 1, 0)
        );
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn a_lane_with_no_consumers_is_not_recovered() {
        // Arrange
        let dir = tempfile::tempdir().expect("temporary directory");
        let settings = file_settings(dir.path(), true);
        let secondary = leave_message_in_progress(&settings, QueueLane::Secondary, ECHO).await;

        // Act
        run_consumers_until(settings, Duration::from_millis(500)).await;

        // Assert
        assert_eq!(
            secondary.lengths().await.expect("lengths"),
            expected_lengths(0, 1, 0)
        );
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn cancellation_during_the_dequeue_error_back_off_stops_promptly() {
        // Arrange
        let dir = tempfile::tempdir().expect("temporary directory");
        let settings = file_settings(dir.path(), false);
        let pending = dir.path().join("InEngineQueue_Primary_Pending");
        let remover = tokio::spawn(async move {
            tokio::time::sleep(Duration::from_millis(200)).await;
            std::fs::remove_dir(pending).expect("remove the pending directory");
        });

        // Act
        let stop_time = run_consumers_until(settings, Duration::from_millis(600)).await;

        // Assert
        remover.await.expect("remover");
        assert!(
            stop_time < Duration::from_secs(1),
            "stopped after {stop_time:?}"
        );
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn cancellation_while_a_settle_waits_to_retry_leaves_the_message_in_progress() {
        use std::os::unix::fs::PermissionsExt;

        // Arrange
        let dir = tempfile::tempdir().expect("temporary directory");
        let settings = file_settings(dir.path(), true);
        let queue = leave_message_in_progress(&settings, QueueLane::Primary, "missing").await;
        let failed = dir.path().join("InEngineQueue_Primary_Failed");
        std::fs::set_permissions(&failed, std::fs::Permissions::from_mode(0o555)).expect("chmod");

        // Act
        let stop_time = run_consumers_until(settings, Duration::from_millis(600)).await;

        // Assert
        std::fs::set_permissions(&failed, std::fs::Permissions::from_mode(0o755)).expect("chmod");
        assert!(
            stop_time < Duration::from_secs(1),
            "stopped after {stop_time:?}"
        );
        assert_eq!(
            queue.lengths().await.expect("lengths"),
            expected_lengths(0, 1, 0)
        );
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn cancellation_while_a_redis_open_retries_stops_promptly() {
        // Arrange
        let dir = tempfile::tempdir().expect("temporary directory");
        let mut settings = file_settings(dir.path(), false);
        settings.queue.queue_driver = crate::config::QueueDriver::Redis;
        settings.queue.redis.port = 1;

        // Act
        let stop_time = run_consumers_until(settings, Duration::from_millis(300)).await;

        // Assert
        assert!(
            stop_time < Duration::from_secs(1),
            "stopped after {stop_time:?}"
        );
    }
}
