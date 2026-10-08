mod dir;

use std::ffi::{OsStr, OsString};
use std::fs;
use std::io;
use std::path::{Path, PathBuf};
use std::time::{Duration, Instant};

use tracing::warn;

use crate::error::QueueError;
use crate::queue::delivery::{Delivery, Receipt};
use crate::queue::envelope::CommandEnvelope;
use crate::queue::handle::{PeekedMessage, QueueLane, QueueLengths, Stage};

const STALE_TEMP_AGE: Duration = Duration::from_secs(60 * 60);
const POLL_INTERVAL: Duration = Duration::from_secs(1);

/// A queue lane stored as one file per message in three directories.
///
/// Every call lists the directory it reads, so the cost of a claim grows with the number of
/// pending messages.
pub(crate) struct FileDriver {
    lane: QueueLane,
    pending: PathBuf,
    in_progress: PathBuf,
    failed: PathBuf,
}

impl FileDriver {
    pub(crate) fn open(
        queue_name: &str,
        lane: QueueLane,
        base_path: &str,
    ) -> Result<Self, QueueError> {
        let base = Path::new(base_path);
        let stage_path =
            |stage: Stage| base.join(format!("{queue_name}_{}_{}", lane.as_str(), stage.as_str()));
        let driver = Self {
            lane,
            pending: stage_path(Stage::Pending),
            in_progress: stage_path(Stage::InProgress),
            failed: stage_path(Stage::Failed),
        };
        for stage in Stage::ALL {
            let dir = driver.stage_dir(stage);
            fs::create_dir_all(dir)?;
            dir::remove_stale_temp_files(dir, STALE_TEMP_AGE)?;
        }
        Ok(driver)
    }

    pub(crate) fn lane(&self) -> QueueLane {
        self.lane
    }

    pub(crate) fn publish(&self, envelope: &CommandEnvelope) -> Result<(), QueueError> {
        let json = serde_json::to_vec_pretty(envelope)?;
        dir::write_atomic(&self.pending, &dir::new_message_name(), &json)?;
        Ok(())
    }

    pub(crate) async fn dequeue(&self, wait: Duration) -> Result<Option<Delivery>, QueueError> {
        let deadline = Instant::now().checked_add(wait);
        loop {
            if let Some(delivery) = self.claim_oldest()? {
                return Ok(Some(delivery));
            }
            let remaining = match deadline {
                Some(deadline) => deadline.saturating_duration_since(Instant::now()),
                None => POLL_INTERVAL,
            };
            if remaining.is_zero() {
                return Ok(None);
            }
            tokio::time::sleep(remaining.min(POLL_INTERVAL)).await;
        }
    }

    /// Returns false when the message is no longer in progress.
    pub(crate) fn acknowledge(&self, name: &OsStr) -> Result<bool, QueueError> {
        Ok(dir::remove_if_present(&self.in_progress.join(name))?)
    }

    /// Returns false when the message is no longer in progress.
    ///
    /// The in-progress file is first renamed to a hidden settling name, which fails when the
    /// message is no longer in progress. A settling file left by an earlier attempt is used
    /// as it is. A crash after the new file is written and before the settling file is
    /// removed leaves two copies, and `recover` returns the older one.
    pub(crate) fn move_claimed(
        &self,
        name: &OsStr,
        envelope: &CommandEnvelope,
        target: Stage,
    ) -> Result<bool, QueueError> {
        let current = self.in_progress.join(name);
        let claimed = self.in_progress.join(dir::settling_name(name));
        if !dir::rename_if_present(&current, &claimed)? && !claimed.exists() {
            return Ok(false);
        }
        let json = serde_json::to_vec_pretty(envelope)?;
        let target_dir = self.stage_dir(target);
        let target_name = dir::new_message_name();
        if let Err(error) = dir::write_atomic(target_dir, &target_name, &json) {
            if let Err(restore_error) = fs::rename(&claimed, &current) {
                warn!("could not restore {}: {restore_error}", claimed.display());
            }
            return Err(error.into());
        }
        match fs::remove_file(&claimed) {
            Ok(()) => Ok(true),
            Err(error) if error.kind() == io::ErrorKind::NotFound => {
                // A clear removed the message while it was being settled, so the new copy
                // must not survive.
                dir::remove_if_present(&target_dir.join(target_name))?;
                Ok(false)
            }
            Err(error) => {
                warn!(
                    "message {} was moved, but {} could not be removed: {error}",
                    envelope.id,
                    claimed.display()
                );
                Ok(true)
            }
        }
    }

    pub(crate) fn recover(&self) -> Result<usize, QueueError> {
        let mut moved = 0;
        for name in dir::list_messages(&self.in_progress)? {
            if dir::rename_if_present(&self.in_progress.join(&name), &self.pending.join(&name))? {
                moved += 1;
            }
        }
        for name in dir::list_settling(&self.in_progress)? {
            let claimed = self.in_progress.join(dir::settling_name(&name));
            if dir::rename_if_present(&claimed, &self.pending.join(&name))? {
                moved += 1;
            }
        }
        Ok(moved)
    }

    pub(crate) fn lengths(&self) -> Result<QueueLengths, QueueError> {
        Ok(QueueLengths {
            pending: self.stage_entries(Stage::Pending)?.len(),
            in_progress: self.stage_entries(Stage::InProgress)?.len(),
            failed: self.stage_entries(Stage::Failed)?.len(),
        })
    }

    pub(crate) fn clear(&self, stage: Stage) -> Result<usize, QueueError> {
        let dir = self.stage_dir(stage);
        let mut removed = 0;
        for name in dir::list_messages(dir)? {
            if dir::remove_if_present(&dir.join(name))? {
                removed += 1;
            }
        }
        if stage == Stage::InProgress {
            removed += dir::remove_settling_files(dir)?;
        }
        dir::remove_stale_temp_files(dir, STALE_TEMP_AGE)?;
        Ok(removed)
    }

    pub(crate) fn peek(
        &self,
        stage: Stage,
        first: usize,
        last: usize,
    ) -> Result<Vec<PeekedMessage>, QueueError> {
        let mut messages = Vec::new();
        let count = last.saturating_sub(first).saturating_add(1);
        for path in self
            .stage_entries(stage)?
            .into_iter()
            .skip(first)
            .take(count)
        {
            match fs::read(path) {
                Ok(raw) => messages.push(PeekedMessage::parse(&raw)),
                Err(error) if error.kind() == io::ErrorKind::NotFound => {}
                Err(error) => return Err(error.into()),
            }
        }
        Ok(messages)
    }

    pub(crate) fn republish_failed(&self, limit: usize) -> Result<usize, QueueError> {
        let mut moved = 0;
        for name in dir::list_messages(&self.failed)?.into_iter().take(limit) {
            let to = self.pending.join(dir::new_message_name());
            if dir::rename_if_present(&self.failed.join(name), &to)? {
                moved += 1;
            }
        }
        Ok(moved)
    }

    fn claim_oldest(&self) -> Result<Option<Delivery>, QueueError> {
        for name in dir::list_messages(&self.pending)? {
            let claimed = self.in_progress.join(&name);
            if !dir::rename_if_present(&self.pending.join(&name), &claimed)? {
                continue;
            }
            let parsed = match fs::read(&claimed) {
                Ok(raw) => {
                    serde_json::from_slice::<CommandEnvelope>(&raw).map_err(QueueError::from)
                }
                Err(error) if error.kind() == io::ErrorKind::NotFound => continue,
                Err(error) => Err(error.into()),
            };
            match parsed {
                Ok(mut envelope) => {
                    envelope.current_try += 1;
                    return Ok(Some(Delivery::new(
                        envelope,
                        self.lane,
                        Receipt::File(name),
                    )));
                }
                Err(error) => self.move_unreadable_to_failed(&claimed, &error),
            }
        }
        Ok(None)
    }

    fn move_unreadable_to_failed(&self, claimed: &Path, error: &QueueError) {
        warn!(
            "moving unreadable message {} to failed: {error}",
            claimed.display()
        );
        if let Err(move_error) =
            dir::rename_if_present(claimed, &self.failed.join(dir::new_message_name()))
        {
            warn!(
                "could not move {} to failed: {move_error}",
                claimed.display()
            );
        }
    }

    /// The paths of the messages in `stage`, oldest first. In-progress also includes the
    /// messages that a settle operation holds.
    fn stage_entries(&self, stage: Stage) -> Result<Vec<PathBuf>, QueueError> {
        let dir = self.stage_dir(stage);
        let mut entries: Vec<(OsString, PathBuf)> = dir::list_messages(dir)?
            .into_iter()
            .map(|name| (name.clone(), dir.join(name)))
            .collect();
        if stage == Stage::InProgress {
            entries.extend(
                dir::list_settling(dir)?
                    .into_iter()
                    .map(|name| (name.clone(), dir.join(dir::settling_name(&name)))),
            );
            entries.sort();
        }
        Ok(entries.into_iter().map(|(_, path)| path).collect())
    }

    fn stage_dir(&self, stage: Stage) -> &Path {
        match stage {
            Stage::Pending => &self.pending,
            Stage::InProgress => &self.in_progress,
            Stage::Failed => &self.failed,
        }
    }
}
