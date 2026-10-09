use std::fmt;
use std::sync::Arc;
use std::time::Duration;

use tokio_util::sync::CancellationToken;

use super::open_queue;
use crate::command::{run_blocking, CommandContext, CommandResult};
use crate::config::{QueueSettings, Settings};
use crate::error::CommandError;
use crate::output::Output;
use crate::queue::{
    process_delivery, Attempt, CommandEnvelope, CommandRegistry, QueueLane, SettleOutcome,
};

pub struct Publish {
    pub plugin_name: String,
    pub command_name: String,
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

    /// Publishes one message and writes a confirmation line to `output`.
    pub async fn run(&self, settings: &QueueSettings, output: &mut Output) -> CommandResult {
        let envelope = CommandEnvelope::new(
            &self.command_name,
            &self.plugin_name,
            &self.payload()?,
            settings.use_compression,
            self.max_retries.unwrap_or(settings.max_retries),
        )
        .map_err(|error| CommandError::failed_with("could not create the envelope", error))?;
        let queue = open_queue(self.lane, settings).await?;
        queue
            .publish(&envelope)
            .await
            .map_err(|error| CommandError::failed_with("could not publish", error))?;
        output.info_line(&format!(
            "published {} from {} to the {} queue",
            self.command_name,
            self.plugin_name,
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
    released: usize,
}

impl ConsumeTally {
    fn record(&mut self, outcome: SettleOutcome) {
        match outcome {
            SettleOutcome::Acknowledged => self.acknowledged += 1,
            SettleOutcome::Retried => self.retried += 1,
            SettleOutcome::MovedToFailed => self.moved_to_failed += 1,
            SettleOutcome::Released => self.released += 1,
        }
    }

    fn incomplete(&self) -> usize {
        self.retried + self.moved_to_failed + self.released
    }
}

impl fmt::Display for ConsumeTally {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(
            f,
            "completed: {}, returned for a retry: {}, moved to failed: {}",
            self.acknowledged, self.retried, self.moved_to_failed
        )?;
        if self.released > 0 {
            write!(
                f,
                ", returned because the server stopped: {}",
                self.released
            )?;
        }
        Ok(())
    }
}

impl Consume {
    /// Runs messages until the limit or until pending is empty. Each message runs on the
    /// blocking thread pool, and its output comes before its status line in `output`.
    pub async fn run(&self, settings: Arc<Settings>, output: &mut Output) -> CommandResult {
        let limit = self.limit.unwrap_or(usize::MAX);
        let queue = open_queue(self.lane, &settings.queue).await?;
        let never_cancelled = CancellationToken::new();
        let mut tally = ConsumeTally::default();
        for _ in 0..limit {
            let dequeued = queue.dequeue(Duration::ZERO).await.map_err(|error| {
                CommandError::failed_with(format!("could not dequeue ({tally})"), error)
            })?;
            let Some(delivery) = dequeued else {
                break;
            };
            let ctx = CommandContext::new(Output::new(output.uses_color()), settings.clone());
            let command_output = &mut *output;
            let processed = process_delivery(
                &queue,
                delivery,
                &self.registry,
                &never_cancelled,
                |command| async move {
                    let (result, text) = run_blocking(Arc::from(command), ctx).await?;
                    command_output.text(&text);
                    result
                },
            )
            .await;
            let id = &processed.message_id;
            match &processed.attempt {
                Attempt::Succeeded => {}
                Attempt::CommandFailed(error) => {
                    output.error_line(&format!("message {id}: {error}"));
                }
                Attempt::Unrunnable(error) => {
                    output.error_line(&format!(
                        "could not build the command for message {id}: {error}"
                    ));
                }
                Attempt::Interrupted => {
                    output.error_line(&format!("message {id}: {}", CommandError::Interrupted));
                }
            }
            let outcome = processed.settled.map_err(|error| {
                CommandError::failed_with(format!("stopped at message {id} ({tally})"), error)
            })?;
            tally.record(outcome);
            output.line(&format!("{id}: {}", outcome.label()));
        }
        if tally.incomplete() > 0 {
            return Err(CommandError::failed(format!(
                "some messages did not complete ({tally})"
            )));
        }
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn tally_names_released_messages_only_when_there_are_some() {
        // Arrange
        let mut tally = ConsumeTally::default();
        tally.record(SettleOutcome::Acknowledged);
        let without_released = tally.to_string();
        tally.record(SettleOutcome::Released);

        // Act
        let with_released = tally.to_string();

        // Assert
        assert_eq!(
            without_released,
            "completed: 1, returned for a retry: 0, moved to failed: 0"
        );
        assert_eq!(
            with_released,
            "completed: 1, returned for a retry: 0, moved to failed: 0, returned because the server stopped: 1"
        );
        assert_eq!(tally.incomplete(), 1);
    }
}
