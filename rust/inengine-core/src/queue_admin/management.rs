use std::ops::RangeInclusive;

use super::open_queue;
use crate::command::CommandResult;
use crate::config::QueueSettings;
use crate::error::CommandError;
use crate::output::Output;
use crate::queue::{PeekedMessage, QueueLane, Stage};

fn selected_stages(pending: bool, in_progress: bool, failed: bool) -> Vec<Stage> {
    [
        (pending, Stage::Pending),
        (in_progress, Stage::InProgress),
        (failed, Stage::Failed),
    ]
    .into_iter()
    .filter_map(|(selected, stage)| selected.then_some(stage))
    .collect()
}

pub struct Clear {
    pub pending: bool,
    pub failed: bool,
    pub in_progress: bool,
    pub lane: QueueLane,
}

impl Clear {
    /// Removes every message from each selected stage.
    pub async fn run(&self, settings: &QueueSettings, output: &mut Output) -> CommandResult {
        let stages = selected_stages(self.pending, self.in_progress, self.failed);
        if stages.is_empty() {
            return Err(CommandError::failed(
                "specify at least one stage to clear: --pending, --failed, or --in-progress"
                    .to_string(),
            ));
        }
        let queue = open_queue(self.lane, settings).await?;
        for stage in stages {
            let removed = queue.clear(stage).await.map_err(|error| {
                CommandError::failed_with(format!("could not clear {}", stage.label()), error)
            })?;
            output.info_line(&format!("{}: removed {removed}", stage.label()));
        }
        Ok(())
    }
}

/// Writes the length of each stage of both lanes.
pub async fn write_lengths(settings: &QueueSettings, output: &mut Output) -> CommandResult {
    for lane in QueueLane::ALL {
        let queue = open_queue(lane, settings).await?;
        let lengths = queue.lengths().await.map_err(|error| {
            CommandError::failed_with("could not read the queue lengths", error)
        })?;
        output.warning_line(&format!("{} Queue:", lane.as_str()));
        for (stage, length) in [
            (Stage::Pending, lengths.pending),
            (Stage::InProgress, lengths.in_progress),
            (Stage::Failed, lengths.failed),
        ] {
            output
                .info(&format!("{:>14}", stage.label()))
                .line(&format!("{length:>10}"));
        }
        output.newlines(1);
    }
    Ok(())
}

pub struct Peek {
    pub range: RangeInclusive<usize>,
    pub json_format: bool,
    pub pending: bool,
    pub failed: bool,
    pub in_progress: bool,
    pub lane: QueueLane,
}

impl Peek {
    /// Writes the messages in `range` of each selected stage.
    pub async fn run(&self, settings: &QueueSettings, output: &mut Output) -> CommandResult {
        let stages = selected_stages(self.pending, self.in_progress, self.failed);
        if stages.is_empty() {
            return Err(CommandError::failed(
                "specify at least one stage to peek: --pending, --failed, or --in-progress"
                    .to_string(),
            ));
        }
        let queue = open_queue(self.lane, settings).await?;
        for stage in stages {
            let messages = queue
                .peek(stage, self.range.clone())
                .await
                .map_err(|error| {
                    CommandError::failed_with(format!("could not peek {}", stage.label()), error)
                })?;
            write_messages(output, stage, &messages, self.json_format)?;
        }
        Ok(())
    }
}

fn write_messages(
    output: &mut Output,
    stage: Stage,
    messages: &[PeekedMessage],
    json: bool,
) -> CommandResult {
    output.warning(stage.label());
    if messages.is_empty() {
        output.line(": no messages available.");
        return Ok(());
    }
    output.newlines(1);
    for message in messages {
        match (message, json) {
            (PeekedMessage::Envelope(envelope), true) => {
                let text = serde_json::to_string(envelope).map_err(|error| {
                    CommandError::failed_with("could not format a message", error)
                })?;
                output.line(&text);
            }
            (PeekedMessage::Malformed { raw, reason }, true) => {
                let value = serde_json::json!({ "malformed": true, "reason": reason, "raw": raw });
                output.line(&value.to_string());
            }
            (PeekedMessage::Envelope(envelope), false) => {
                output.info("  id: ").line(&envelope.id);
                output.info("  command: ").line(&envelope.command_name);
                output
                    .info("  queued: ")
                    .line(&envelope.queued_at.to_string());
                output.newlines(1);
            }
            (PeekedMessage::Malformed { raw, reason }, false) => {
                output.error("  malformed: ").line(reason);
                output.line(raw);
                output.newlines(1);
            }
        }
    }
    Ok(())
}

pub struct RepublishFailed {
    pub limit: usize,
    pub lane: QueueLane,
}

impl RepublishFailed {
    /// Moves up to `limit` failed messages back to pending.
    pub async fn run(&self, settings: &QueueSettings, output: &mut Output) -> CommandResult {
        let queue = open_queue(self.lane, settings).await?;
        let count = queue
            .republish_failed(self.limit)
            .await
            .map_err(|error| CommandError::failed_with("could not republish", error))?;
        output.info_line(&format!(
            "failed messages returned to pending in the {} queue: {count}",
            self.lane.as_str()
        ));
        Ok(())
    }
}

pub struct Recover {
    pub lane: QueueLane,
}

impl Recover {
    /// Moves every in-progress message back to pending.
    pub async fn run(&self, settings: &QueueSettings, output: &mut Output) -> CommandResult {
        let queue = open_queue(self.lane, settings).await?;
        let count = queue
            .recover()
            .await
            .map_err(|error| CommandError::failed_with("could not recover", error))?;
        output.info_line(&format!(
            "in-progress messages returned to pending in the {} queue: {count}",
            self.lane.as_str()
        ));
        Ok(())
    }
}
