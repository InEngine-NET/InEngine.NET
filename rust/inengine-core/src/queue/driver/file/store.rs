use std::ffi::{OsStr, OsString};
use std::fs;
use std::io;
use std::path::{Path, PathBuf};
use std::time::Duration;

use tracing::warn;

use super::dir;
use crate::error::{ErrorChain, QueueError};
use crate::queue::CommandEnvelope;
use crate::queue::delivery::{Delivery, Receipt};
use crate::queue::{PeekedMessage, QueueLane, QueueLengths, Stage};

const STALE_TEMP_AGE: Duration = Duration::from_secs(60 * 60);

/// The synchronous file queue. Each claim lists the pending directory, so a claim gets slower
/// as the backlog grows.
pub(super) struct FileStore {
    lane: QueueLane,
    pending: PathBuf,
    in_progress: PathBuf,
    failed: PathBuf,
}

impl FileStore {
    pub(super) fn open(queue_name: &str, lane: QueueLane, base: &Path) -> Result<Self, QueueError> {
        let stage_path =
            |stage: Stage| base.join(format!("{queue_name}_{}_{}", lane.as_str(), stage.as_str()));
        let store = Self {
            lane,
            pending: stage_path(Stage::Pending),
            in_progress: stage_path(Stage::InProgress),
            failed: stage_path(Stage::Failed),
        };
        for stage in Stage::ALL {
            let dir = store.stage_dir(stage);
            fs::create_dir_all(dir)?;
            dir::remove_stale_temp_files(dir, STALE_TEMP_AGE)?;
        }
        Ok(store)
    }

    pub(super) fn publish(&self, envelope: &CommandEnvelope) -> Result<(), QueueError> {
        let json = serde_json::to_vec_pretty(envelope)?;
        dir::write_atomic(&self.pending, &dir::new_message_name(), &json)?;
        Ok(())
    }

    /// Returns false when the message is no longer in progress.
    pub(super) fn acknowledge(&self, name: &OsStr) -> Result<bool, QueueError> {
        Ok(dir::remove_if_present(&self.in_progress.join(name))?)
    }

    /// Returns false when the message is no longer in progress.
    ///
    /// The in-progress file is first renamed to a hidden settling name, which fails when the
    /// message is no longer in progress. When an earlier settle of the same message stopped
    /// partway, its settling file is reused. A crash after the new file is written and before
    /// the settling file is removed leaves two copies, and `recover` returns the older one.
    pub(super) fn move_claimed(
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
                warn!(
                    "could not restore {}: {}",
                    claimed.display(),
                    ErrorChain::new(&restore_error)
                );
            }
            return Err(error.into());
        }
        match fs::remove_file(&claimed) {
            Ok(()) => Ok(true),
            Err(error) if error.kind() == io::ErrorKind::NotFound => {
                // A flush removed the message during this settle, so remove the new copy too.
                dir::remove_if_present(&target_dir.join(target_name))?;
                Ok(false)
            }
            Err(error) => {
                warn!(
                    "message {} was moved, but {} could not be removed: {}",
                    envelope.id,
                    claimed.display(),
                    ErrorChain::new(&error)
                );
                Ok(true)
            }
        }
    }

    pub(super) fn recover(&self) -> Result<usize, QueueError> {
        let mut moved = 0;
        for name in dir::list_messages(&self.in_progress)? {
            if dir::rename_if_present(&self.in_progress.join(&name), &self.pending.join(&name))? {
                moved += 1;
            }
        }
        for name in dir::unfinished_settles(&self.in_progress)? {
            let claimed = self.in_progress.join(dir::settling_name(&name));
            if dir::rename_if_present(&claimed, &self.pending.join(&name))? {
                moved += 1;
            }
        }
        Ok(moved)
    }

    pub(super) fn lengths(&self) -> Result<QueueLengths, QueueError> {
        Ok(QueueLengths {
            pending: self.stage_paths(Stage::Pending)?.len(),
            in_progress: self.stage_paths(Stage::InProgress)?.len(),
            failed: self.stage_paths(Stage::Failed)?.len(),
        })
    }

    pub(super) fn clear(&self, stage: Stage) -> Result<usize, QueueError> {
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

    pub(super) fn peek(
        &self,
        stage: Stage,
        first: usize,
        last: usize,
    ) -> Result<Vec<PeekedMessage>, QueueError> {
        let mut messages = Vec::new();
        let count = last.saturating_sub(first).saturating_add(1);
        for path in self.stage_paths(stage)?.into_iter().skip(first).take(count) {
            match fs::read(path) {
                Ok(raw) => messages.push(PeekedMessage::parse(&raw)),
                Err(error) if error.kind() == io::ErrorKind::NotFound => {}
                Err(error) => return Err(error.into()),
            }
        }
        Ok(messages)
    }

    pub(super) fn republish_failed(&self, limit: usize) -> Result<usize, QueueError> {
        let mut moved = 0;
        for name in dir::list_messages(&self.failed)?.into_iter().take(limit) {
            let to = self.pending.join(dir::new_message_name());
            if dir::rename_if_present(&self.failed.join(name), &to)? {
                moved += 1;
            }
        }
        Ok(moved)
    }

    /// Claims the oldest pending message, or returns `None` when there is none.
    pub(super) fn claim_oldest(&self) -> Result<Option<Delivery>, QueueError> {
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
                    envelope.attempts += 1;
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
            "moving unreadable message {} to failed: {}",
            claimed.display(),
            ErrorChain::new(error)
        );
        if let Err(move_error) =
            dir::rename_if_present(claimed, &self.failed.join(dir::new_message_name()))
        {
            warn!(
                "could not move {} to failed: {}",
                claimed.display(),
                ErrorChain::new(&move_error)
            );
        }
    }

    fn stage_paths(&self, stage: Stage) -> Result<Vec<PathBuf>, QueueError> {
        let dir = self.stage_dir(stage);
        let mut entries: Vec<(OsString, PathBuf)> = dir::list_messages(dir)?
            .into_iter()
            .map(|name| (name.clone(), dir.join(name)))
            .collect();
        if stage == Stage::InProgress {
            entries.extend(
                dir::unfinished_settles(dir)?
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
