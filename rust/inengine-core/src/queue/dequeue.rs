use std::sync::Arc;
use std::time::Duration;

use tokio_util::sync::CancellationToken;
use tracing::{debug, error, info};

use crate::config::{QueueSettings, Settings};
use crate::queue::client::{make_queue_client, QueueClientDyn};
use crate::queue::envelope::CommandEnvelope;
use crate::queue::registry::CommandRegistry;
use crate::server::lifecycle::{run_with_lifecycle, CommandLifecycle};

pub struct Dequeue {
    pub queue_settings: QueueSettings,
    pub settings: Arc<Settings>,
    pub registry: Arc<CommandRegistry>,
}

impl Dequeue {
    pub async fn start(&self, cancel: CancellationToken) {
        let mut handles = Vec::new();

        for i in 0..self.queue_settings.primary_queue_consumers {
            let client = make_queue_client(false, &self.queue_settings);
            match client {
                Ok(c) => {
                    let settings = self.settings.clone();
                    let registry = self.registry.clone();
                    let token = cancel.clone();
                    handles.push(tokio::spawn(async move {
                        consume_loop(i, c, settings, registry, token).await;
                    }));
                }
                Err(e) => error!("failed to create primary queue client {i}: {e}"),
            }
        }

        for i in 0..self.queue_settings.secondary_queue_consumers {
            let client = make_queue_client(true, &self.queue_settings);
            match client {
                Ok(c) => {
                    let settings = self.settings.clone();
                    let registry = self.registry.clone();
                    let token = cancel.clone();
                    handles.push(tokio::spawn(async move {
                        consume_loop(i, c, settings, registry, token).await;
                    }));
                }
                Err(e) => error!("failed to create secondary queue client {i}: {e}"),
            }
        }

        for handle in handles {
            let _ = handle.await;
        }
    }
}

async fn consume_loop(
    id: usize,
    client: Box<dyn QueueClientDyn>,
    settings: Arc<Settings>,
    registry: Arc<CommandRegistry>,
    cancel: CancellationToken,
) {
    debug!("consumer {id} started for {} queue", client.queue_name());

    loop {
        if cancel.is_cancelled() {
            debug!("consumer {id} stopping");
            return;
        }

        let envelope = tokio::select! {
            _ = cancel.cancelled() => return,
            result = client.dequeue_dyn() => {
                match result {
                    Ok(Some(env)) => env,
                    Ok(None) => {
                        tokio::select! {
                            _ = cancel.cancelled() => return,
                            _ = tokio::time::sleep(Duration::from_secs(5)) => continue,
                        }
                    }
                    Err(e) => {
                        error!("consumer {id} dequeue error: {e}");
                        tokio::time::sleep(Duration::from_secs(5)).await;
                        continue;
                    }
                }
            }
        };

        let mut envelope = envelope;
        envelope.current_try += 1;

        let cmd = match envelope.to_command(&registry) {
            Ok(c) => c,
            Err(e) => {
                error!("consumer {id} failed to construct command: {e}");
                move_to_failed(&client, &envelope).await;
                continue;
            }
        };

        let cmd_name = cmd.name().to_string();
        info!("consumer {id} running: {cmd_name}");
        let lifecycle = CommandLifecycle::default();
        let cmd_arc: Arc<dyn crate::command::Command> = Arc::from(cmd);

        match run_with_lifecycle(cmd_arc, settings.clone(), &lifecycle).await {
            Ok(()) => {
                debug!("consumer {id} completed: {cmd_name}");
                remove_from_in_progress(&client, &envelope).await;
            }
            Err(e) => {
                error!("consumer {id} command failed: {e}");
                if envelope.should_retry() {
                    debug!("consumer {id} re-enqueueing (try {})", envelope.current_try);
                    let updated_json = serde_json::to_string(&envelope).ok();
                    if let Some(json) = updated_json {
                        let re_envelope: Result<CommandEnvelope, _> = serde_json::from_str(&json);
                        if let Ok(re_env) = re_envelope {
                            let _ = client.publish_dyn(&re_env).await;
                        }
                    }
                    remove_from_in_progress(&client, &envelope).await;
                } else {
                    move_to_failed(&client, &envelope).await;
                }
            }
        }
    }
}

async fn remove_from_in_progress(
    client: &Box<dyn QueueClientDyn>,
    envelope: &CommandEnvelope,
) {
    if let Err(e) = client.remove_in_progress_dyn(envelope).await {
        error!("failed to remove envelope {} from in-progress: {e}", envelope.id);
    }
}

async fn move_to_failed(
    client: &Box<dyn QueueClientDyn>,
    envelope: &CommandEnvelope,
) {
    if let Err(e) = client.move_to_failed_dyn(envelope).await {
        error!("failed to move envelope {} to failed: {e}", envelope.id);
    }
}
