use std::ops::RangeInclusive;

use super::{block_on, open_queue};
use crate::command::{Command, CommandContext, CommandResult};
use crate::error::CommandError;
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

impl Command for Clear {
    fn name(&self) -> &str {
        "queue:flush"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let stages = selected_stages(self.pending, self.in_progress, self.failed);
        if stages.is_empty() {
            return Err(CommandError::failed(
                "specify at least one stage to clear: --pending, --failed, or --in-progress"
                    .to_string(),
            ));
        }
        let settings = ctx.settings.clone();
        block_on(async {
            let queue = open_queue(self.lane, &settings.queue).await?;
            for stage in stages {
                let removed = queue.clear(stage).await.map_err(|error| {
                    CommandError::failed_with(format!("could not clear {}", stage.label()), error)
                })?;
                ctx.output
                    .info_line(&format!("{}: removed {removed}", stage.label()));
            }
            Ok(())
        })
    }
}

pub struct Length;

impl Command for Length {
    fn name(&self) -> &str {
        "queue:length"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        print_lengths(ctx)
    }
}

/// An alias of [`Length`].
pub struct Status;

impl Command for Status {
    fn name(&self) -> &str {
        "queue:status"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        print_lengths(ctx)
    }
}

fn print_lengths(ctx: &mut CommandContext) -> CommandResult {
    let settings = ctx.settings.clone();
    block_on(async {
        for lane in QueueLane::ALL {
            let queue = open_queue(lane, &settings.queue).await?;
            let lengths = queue.lengths().await.map_err(|error| {
                CommandError::failed_with("could not read the queue lengths", error)
            })?;
            ctx.output
                .warning_line(&format!("{} Queue:", lane.as_str()));
            for (stage, length) in [
                (Stage::Pending, lengths.pending),
                (Stage::InProgress, lengths.in_progress),
                (Stage::Failed, lengths.failed),
            ] {
                ctx.output
                    .info(&format!("{:>14}", stage.label()))
                    .line(&format!("{length:>10}"));
            }
            ctx.output.newlines(1);
        }
        Ok(())
    })
}

pub struct Peek {
    pub range: RangeInclusive<usize>,
    pub json_format: bool,
    pub pending: bool,
    pub failed: bool,
    pub in_progress: bool,
    pub lane: QueueLane,
}

impl Command for Peek {
    fn name(&self) -> &str {
        "queue:peek"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let stages = selected_stages(self.pending, self.in_progress, self.failed);
        if stages.is_empty() {
            return Err(CommandError::failed(
                "specify at least one stage to peek: --pending, --failed, or --in-progress"
                    .to_string(),
            ));
        }
        let settings = ctx.settings.clone();
        block_on(async {
            let queue = open_queue(self.lane, &settings.queue).await?;
            for stage in stages {
                let messages = queue
                    .peek(stage, self.range.clone())
                    .await
                    .map_err(|error| {
                        CommandError::failed_with(
                            format!("could not peek {}", stage.label()),
                            error,
                        )
                    })?;
                print_messages(ctx, stage, &messages, self.json_format)?;
            }
            Ok(())
        })
    }
}

fn print_messages(
    ctx: &mut CommandContext,
    stage: Stage,
    messages: &[PeekedMessage],
    json: bool,
) -> CommandResult {
    ctx.output.warning(stage.label());
    if messages.is_empty() {
        ctx.output.line(": no messages available.");
        return Ok(());
    }
    ctx.output.newlines(1);
    for message in messages {
        match (message, json) {
            (PeekedMessage::Envelope(envelope), true) => {
                let text = serde_json::to_string(envelope).map_err(|error| {
                    CommandError::failed_with("could not format a message", error)
                })?;
                ctx.output.line(&text);
            }
            (PeekedMessage::Malformed { raw, reason }, true) => {
                let value = serde_json::json!({ "malformed": true, "reason": reason, "raw": raw });
                ctx.output.line(&value.to_string());
            }
            (PeekedMessage::Envelope(envelope), false) => {
                ctx.output.info("  id: ").line(&envelope.id);
                ctx.output.info("  command: ").line(&envelope.command_name);
                ctx.output
                    .info("  queued: ")
                    .line(&envelope.queued_at.to_string());
                ctx.output.newlines(1);
            }
            (PeekedMessage::Malformed { raw, reason }, false) => {
                ctx.output.error("  malformed: ").line(reason);
                ctx.output.line(raw);
                ctx.output.newlines(1);
            }
        }
    }
    Ok(())
}

pub struct RepublishFailed {
    pub limit: usize,
    pub lane: QueueLane,
}

impl Command for RepublishFailed {
    fn name(&self) -> &str {
        "queue:republish"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let settings = ctx.settings.clone();
        let count = block_on(async {
            let queue = open_queue(self.lane, &settings.queue).await?;
            queue
                .republish_failed(self.limit)
                .await
                .map_err(|error| CommandError::failed_with("could not republish", error))
        })?;
        ctx.output.info_line(&format!(
            "failed messages returned to pending in the {} queue: {count}",
            self.lane.as_str()
        ));
        Ok(())
    }
}

pub struct Recover {
    pub lane: QueueLane,
}

impl Command for Recover {
    fn name(&self) -> &str {
        "queue:recover"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let settings = ctx.settings.clone();
        let count = block_on(async {
            let queue = open_queue(self.lane, &settings.queue).await?;
            queue
                .recover()
                .await
                .map_err(|error| CommandError::failed_with("could not recover", error))
        })?;
        ctx.output.info_line(&format!(
            "in-progress messages returned to pending in the {} queue: {count}",
            self.lane.as_str()
        ));
        Ok(())
    }
}
