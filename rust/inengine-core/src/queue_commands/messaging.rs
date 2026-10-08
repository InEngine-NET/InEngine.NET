use crate::command::{Command, CommandContext, CommandResult};
use crate::error::CommandError;
use crate::queue::client::make_queue_client;
use crate::queue::envelope::CommandEnvelope;
use crate::queue::registry::CommandRegistry;

pub struct Publish {
    pub plugin_name: String,
    pub command_class: Option<String>,
    pub command_verb: Option<String>,
    pub args: Vec<String>,
    pub use_secondary: bool,
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
            .ok_or_else(|| CommandError::Failed {
                message: "specify --class or --command".to_string(),
            })?;

        let mut fields: serde_json::Map<String, serde_json::Value> = serde_json::Map::new();
        for arg in &self.args {
            if let Some((key, value)) = arg.split_once('=') {
                fields.insert(key.to_string(), serde_json::Value::String(value.to_string()));
            }
        }
        let json = serde_json::to_string(&fields).map_err(|e| CommandError::FailedWithSource {
            message: "failed to serialize args".to_string(),
            source: Box::new(e),
        })?;

        let envelope =
            CommandEnvelope::from_fields(class_name, &self.plugin_name, &json, ctx.settings.queue.use_compression)
                .map_err(|e| CommandError::Failed {
                    message: format!("failed to create envelope: {e}"),
                })?;

        let client =
            make_queue_client(self.use_secondary, &ctx.settings.queue).map_err(|e| {
                CommandError::Failed {
                    message: format!("failed to create queue client: {e}"),
                }
            })?;

        tokio::task::block_in_place(|| {
            tokio::runtime::Handle::current().block_on(async {
                client.publish_dyn(&envelope).await.map_err(|e| CommandError::Failed {
                    message: format!("failed to publish: {e}"),
                })
            })
        })?;

        ctx.output
            .info(&format!("published {class_name} to queue"));
        Ok(())
    }
}

pub struct Consume {
    pub consume_all: bool,
    pub count: usize,
    pub use_secondary: bool,
}

impl Command for Consume {
    fn name(&self) -> &str {
        "queue:consume"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let client =
            make_queue_client(self.use_secondary, &ctx.settings.queue).map_err(|e| {
                CommandError::Failed {
                    message: format!("failed to create queue client: {e}"),
                }
            })?;
        let registry = CommandRegistry::new();

        tokio::task::block_in_place(|| {
            tokio::runtime::Handle::current().block_on(async {
                let limit = if self.consume_all { usize::MAX } else { self.count };
                for _ in 0..limit {
                    match consume_one(&client, &registry, ctx).await {
                        ConsumeResult::Processed => {}
                        ConsumeResult::Empty => break,
                        ConsumeResult::Error => break,
                    }
                }
            })
        });

        Ok(())
    }
}

enum ConsumeResult {
    Processed,
    Empty,
    Error,
}

async fn consume_one(
    client: &Box<dyn crate::queue::client::QueueClientDyn>,
    registry: &CommandRegistry,
    ctx: &mut CommandContext,
) -> ConsumeResult {
    match client.dequeue_dyn().await {
        Ok(Some(mut envelope)) => {
            envelope.current_try += 1;
            match envelope.to_command(registry) {
                Ok(cmd) => {
                    if let Err(e) = cmd.run(ctx) {
                        ctx.output.error(&format!("command failed: {e}"));
                    }
                }
                Err(e) => {
                    ctx.output.error(&format!("failed to construct command: {e}"));
                }
            }
            ConsumeResult::Processed
        }
        Ok(None) => ConsumeResult::Empty,
        Err(e) => {
            ctx.output.error(&format!("dequeue error: {e}"));
            ConsumeResult::Error
        }
    }
}
