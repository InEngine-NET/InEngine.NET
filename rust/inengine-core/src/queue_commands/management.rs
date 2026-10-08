use crate::command::{Command, CommandContext, CommandResult};
use crate::error::CommandError;
use crate::queue::client::make_queue_client;

pub struct Flush {
    pub pending: bool,
    pub failed: bool,
    pub in_progress: bool,
    pub use_secondary: bool,
}

impl Command for Flush {
    fn name(&self) -> &str {
        "queue:flush"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        if !self.pending && !self.failed && !self.in_progress {
            return Err(CommandError::Failed {
                message: "specify at least one queue to clear: --pending, --failed, or --in-progress".to_string(),
            });
        }
        let client =
            make_queue_client(self.use_secondary, &ctx.settings.queue).map_err(|e| {
                CommandError::Failed {
                    message: format!("failed to create queue client: {e}"),
                }
            })?;

        tokio::task::block_in_place(|| {
            tokio::runtime::Handle::current().block_on(async {
                if self.pending {
                    let result = client.clear_pending_dyn().await.map_err(|e| CommandError::Failed {
                        message: format!("failed to clear pending: {e}"),
                    })?;
                    ctx.output.info(&format!("pending: {result}"));
                }
                if self.in_progress {
                    let result = client.clear_in_progress_dyn().await.map_err(|e| CommandError::Failed {
                        message: format!("failed to clear in-progress: {e}"),
                    })?;
                    ctx.output.info(&format!("in-progress: {result}"));
                }
                if self.failed {
                    let result = client.clear_failed_dyn().await.map_err(|e| CommandError::Failed {
                        message: format!("failed to clear failed: {e}"),
                    })?;
                    ctx.output.info(&format!("failed: {result}"));
                }
                Ok(())
            })
        })
    }
}

pub struct Length {
    pub use_secondary: bool,
}

impl Length {
    fn print_lengths(ctx: &mut CommandContext, label: &str, use_secondary: bool) -> CommandResult {
        let client = make_queue_client(use_secondary, &ctx.settings.queue).map_err(|e| {
            CommandError::Failed {
                message: format!("failed to create queue client: {e}"),
            }
        })?;

        tokio::task::block_in_place(|| {
            tokio::runtime::Handle::current().block_on(async {
                let lengths = client.queue_lengths_dyn().await.map_err(|e| CommandError::Failed {
                    message: format!("failed to get queue lengths: {e}"),
                })?;
                ctx.output.warning(label);
                ctx.output
                    .info_text("       pending")
                    .line(&format!("{:>10}", lengths.pending));
                ctx.output
                    .info_text("   in-progress")
                    .line(&format!("{:>10}", lengths.in_progress));
                ctx.output
                    .info_text("        failed")
                    .line(&format!("{:>10}", lengths.failed));
                ctx.output.newline(1);
                Ok(())
            })
        })
    }
}

impl Command for Length {
    fn name(&self) -> &str {
        "queue:length"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        Length::print_lengths(ctx, "Primary Queue:", false)?;
        Length::print_lengths(ctx, "Secondary Queue:", true)?;
        Ok(())
    }
}

pub struct Status;

impl Command for Status {
    fn name(&self) -> &str {
        "queue:status"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        Length::print_lengths(ctx, "Primary Queue:", false)?;
        Length::print_lengths(ctx, "Secondary Queue:", true)?;
        Ok(())
    }
}

pub struct Peek {
    pub from: usize,
    pub to: usize,
    pub json_format: bool,
    pub pending: bool,
    pub failed: bool,
    pub in_progress: bool,
    pub use_secondary: bool,
}

impl Command for Peek {
    fn name(&self) -> &str {
        "queue:peek"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        if !self.pending && !self.failed && !self.in_progress {
            return Err(CommandError::Failed {
                message: "specify at least one queue to peek: --pending, --failed, or --in-progress"
                    .to_string(),
            });
        }

        let client =
            make_queue_client(self.use_secondary, &ctx.settings.queue).map_err(|e| {
                CommandError::Failed {
                    message: format!("failed to create queue client: {e}"),
                }
            })?;

        tokio::task::block_in_place(|| {
            tokio::runtime::Handle::current().block_on(async {
                if self.pending {
                    let msgs = client
                        .peek_pending_dyn(self.from, self.to)
                        .await
                        .map_err(|e| CommandError::Failed {
                            message: format!("failed to peek pending: {e}"),
                        })?;
                    print_messages(ctx, "Pending", &msgs, self.json_format);
                }
                if self.in_progress {
                    let msgs = client
                        .peek_in_progress_dyn(self.from, self.to)
                        .await
                        .map_err(|e| CommandError::Failed {
                            message: format!("failed to peek in-progress: {e}"),
                        })?;
                    print_messages(ctx, "In-progress", &msgs, self.json_format);
                }
                if self.failed {
                    let msgs = client
                        .peek_failed_dyn(self.from, self.to)
                        .await
                        .map_err(|e| CommandError::Failed {
                            message: format!("failed to peek failed: {e}"),
                        })?;
                    print_messages(ctx, "Failed", &msgs, self.json_format);
                }
                Ok(())
            })
        })
    }
}

fn print_messages(
    ctx: &mut CommandContext,
    label: &str,
    messages: &[crate::queue::envelope::CommandEnvelope],
    json_format: bool,
) {
    ctx.output.warning_text(label);
    if messages.is_empty() {
        ctx.output.line(" no messages available.");
        return;
    }
    ctx.output.newline(1);
    for msg in messages {
        if json_format {
            if let Ok(json) = serde_json::to_string_pretty(msg) {
                ctx.output.line(&json);
            }
        } else {
            ctx.output
                .info_text("  id: ")
                .line(&msg.id);
            ctx.output
                .info_text("  command: ")
                .line(&msg.command_class_name);
            ctx.output
                .info_text("  queued: ")
                .line(&msg.queued_at.to_string());
            ctx.output.newline(1);
        }
    }
}

pub struct RepublishFailed {
    pub limit: usize,
    pub use_secondary: bool,
}

impl Command for RepublishFailed {
    fn name(&self) -> &str {
        "queue:republish"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let client =
            make_queue_client(self.use_secondary, &ctx.settings.queue).map_err(|e| {
                CommandError::Failed {
                    message: format!("failed to create queue client: {e}"),
                }
            })?;

        tokio::task::block_in_place(|| {
            tokio::runtime::Handle::current().block_on(async {
                let count = client
                    .republish_failed_dyn(self.limit)
                    .await
                    .map_err(|e| CommandError::Failed {
                        message: format!("failed to republish: {e}"),
                    })?;
                ctx.output
                    .info(&format!("republished {count} failed messages"));
                Ok(())
            })
        })
    }
}
