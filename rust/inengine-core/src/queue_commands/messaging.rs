use std::fmt;
use std::sync::Arc;
use std::time::Duration;

use super::{block_on, open_queue};
use crate::command::{Command, CommandContext, CommandResult};
use crate::error::CommandError;
use crate::queue::{
    process_delivery, Attempt, CommandEnvelope, CommandRegistry, QueueLane, SettleOutcome,
};

pub struct Publish {
    pub plugin_name: String,
    pub command_class: Option<String>,
    pub command_verb: Option<String>,
    /// Payload fields as `key=value` pairs. Each value is sent as a string.
    pub args: Vec<String>,
    /// The whole payload as a JSON object, used in place of `args`.
    pub json: Option<String>,
    /// Overrides `queue.max_retries` for this message.
    pub max_retries: Option<u32>,
    pub lane: QueueLane,
}

impl Publish {
    fn payload(&self) -> Result<String, CommandError> {
        if let Some(json) = &self.json {
            serde_json::from_str::<serde_json::Map<String, serde_json::Value>>(json).map_err(
                |error| CommandError::failed_with("the payload is not a JSON object", error),
            )?;
            return Ok(json.clone());
        }
        let fields: serde_json::Map<String, serde_json::Value> = self
            .args
            .iter()
            .filter_map(|arg| arg.split_once('='))
            .map(|(key, value)| {
                (
                    key.to_string(),
                    serde_json::Value::String(value.to_string()),
                )
            })
            .collect();
        serde_json::to_string(&fields)
            .map_err(|error| CommandError::failed_with("could not serialize args", error))
    }
}

impl Command for Publish {
    fn name(&self) -> &str {
        "queue:publish"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let class_name = self
            .command_class
            .as_deref()
            .or(self.command_verb.as_deref())
            .ok_or_else(|| CommandError::failed("specify --class or --command"))?;
        let settings = &ctx.settings.queue;
        let envelope = CommandEnvelope::new(
            class_name,
            &self.plugin_name,
            &self.payload()?,
            settings.use_compression,
            self.max_retries.unwrap_or(settings.max_retries),
        )
        .map_err(|error| CommandError::failed_with("could not create the envelope", error))?;
        block_on(async {
            let queue = open_queue(self.lane, settings).await?;
            queue
                .publish(&envelope)
                .await
                .map_err(|error| CommandError::failed_with("could not publish", error))
        })?;
        ctx.output.info_line(&format!(
            "published {class_name} to the {} queue",
            self.lane.as_str()
        ));
        Ok(())
    }
}

pub struct Consume {
    /// `None` consumes until the pending stage is empty.
    pub limit: Option<usize>,
    pub lane: QueueLane,
    pub registry: Arc<CommandRegistry>,
}

#[derive(Debug, Default)]
struct ConsumeTally {
    acknowledged: usize,
    retried: usize,
    moved_to_failed: usize,
}

impl ConsumeTally {
    fn record(&mut self, outcome: SettleOutcome) {
        match outcome {
            SettleOutcome::Acknowledged => self.acknowledged += 1,
            SettleOutcome::Retried => self.retried += 1,
            SettleOutcome::MovedToFailed => self.moved_to_failed += 1,
        }
    }

    fn incomplete(&self) -> usize {
        self.retried + self.moved_to_failed
    }
}

impl fmt::Display for ConsumeTally {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(
            f,
            "completed: {}, returned for a retry: {}, moved to failed: {}",
            self.acknowledged, self.retried, self.moved_to_failed
        )
    }
}

impl Command for Consume {
    fn name(&self) -> &str {
        "queue:consume"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let settings = ctx.settings.clone();
        let limit = self.limit.unwrap_or(usize::MAX);
        block_on(async {
            let queue = open_queue(self.lane, &settings.queue).await?;
            let mut tally = ConsumeTally::default();
            for _ in 0..limit {
                let dequeued = queue.dequeue(Duration::ZERO).await.map_err(|error| {
                    CommandError::failed_with(format!("could not dequeue ({tally})"), error)
                })?;
                let Some(delivery) = dequeued else {
                    break;
                };
                let processed = process_delivery(&queue, delivery, &self.registry, |command| {
                    std::future::ready(command.run(ctx))
                })
                .await;
                let id = &processed.message_id;
                match &processed.attempt {
                    Attempt::Succeeded => {}
                    Attempt::CommandFailed(error) => {
                        ctx.output.error_line(&format!("message {id}: {error}"));
                    }
                    Attempt::Unrunnable(error) => {
                        ctx.output.error_line(&format!(
                            "could not build the command for message {id}: {error}"
                        ));
                    }
                }
                let outcome = processed.settled.map_err(|error| {
                    CommandError::failed_with(format!("stopped at message {id} ({tally})"), error)
                })?;
                tally.record(outcome);
                ctx.output.line(&format!("{id}: {}", outcome.label()));
            }
            if tally.incomplete() > 0 {
                return Err(CommandError::failed(format!(
                    "some messages did not complete ({tally})"
                )));
            }
            Ok(())
        })
    }
}
