#![allow(dead_code)]

pub mod contract;

use std::collections::HashMap;
use std::path::{Path, PathBuf};
use std::time::Duration;

use inengine_core::commands::echo::Echo;
use inengine_core::config::{self, PluginLimitsSettings, Settings, SettingsSpec};
use inengine_core::plugin::{PluginLoader, PluginRuntime};
use inengine_core::queue::{CommandEnvelope, Delivery, PeekedMessage, Queue, QueueLengths};

pub trait QueueFixture {
    fn queue(&self) -> &Queue;

    async fn open_other_lane(&self) -> Queue;

    /// Adds an element that is not an envelope, so that it is claimed before any later message.
    fn insert_malformed_pending(&self, raw: &[u8]);

    /// Adds an element that the driver cannot read, so that it is claimed before any later
    /// message.
    fn insert_unreadable_pending(&self);
}

pub fn default_settings() -> Settings {
    config::from_spec(&SettingsSpec::default())
        .settings
        .expect("the default settings are valid")
}

pub fn settings_with_queue_base(base: &Path) -> Settings {
    let mut settings = default_settings();
    settings.queue.file.base_path = base.to_str().expect("utf-8 path").to_string();
    settings
}

/// A directory under `tests/fixtures` that holds one plugin component.
pub fn fixture_directory(name: &str) -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("tests/fixtures")
        .join(name)
}

/// The `[plugins]` map with one entry.
pub fn plugin_directories(plugin_name: &str, directory: &Path) -> HashMap<String, String> {
    HashMap::from([(
        plugin_name.to_string(),
        directory.to_string_lossy().into_owned(),
    )])
}

/// Loads the fixture plugin under the name `fixture`.
pub fn load_fixture_plugin(limits: PluginLimitsSettings) -> PluginLoader {
    let runtime = PluginRuntime::new(limits).expect("runtime");
    let directories = plugin_directories("fixture", &fixture_directory("plugin"));
    let (loader, errors) = PluginLoader::load(&directories, runtime);
    assert!(errors.is_empty(), "{errors:?}");
    loader
}

pub fn expected_lengths(pending: usize, in_progress: usize, failed: usize) -> QueueLengths {
    QueueLengths {
        pending,
        in_progress,
        failed,
    }
}

pub fn echo_envelope(text: &str, max_retries: u32) -> CommandEnvelope {
    CommandEnvelope::from_command(
        &Echo {
            text: text.to_string(),
        },
        false,
        max_retries,
    )
    .expect("envelope")
}

pub async fn publish_echo(queue: &Queue, text: &str, max_retries: u32) -> String {
    let envelope = echo_envelope(text, max_retries);
    queue.publish(&envelope).await.expect("publish");
    envelope.id
}

pub async fn claim(queue: &Queue) -> Delivery {
    queue
        .dequeue(Duration::ZERO)
        .await
        .expect("dequeue")
        .expect("a pending message")
}

pub async fn drain_ids(queue: &Queue) -> Vec<String> {
    let mut ids = Vec::new();
    while let Some(delivery) = queue.dequeue(Duration::ZERO).await.expect("dequeue") {
        ids.push(delivery.envelope.id.clone());
        queue.acknowledge(delivery).await.expect("acknowledge");
    }
    ids
}

pub fn peeked_ids(messages: &[PeekedMessage]) -> Vec<String> {
    messages
        .iter()
        .map(|message| match message {
            PeekedMessage::Envelope(envelope) => envelope.id.clone(),
            PeekedMessage::Malformed { reason, .. } => format!("malformed: {reason}"),
        })
        .collect()
}

/// Defines one test for each queue contract case. `$open` is an async expression that
/// returns a new [`QueueFixture`].
#[allow(unused_macros)]
macro_rules! queue_contract_tests {
    ($open:expr) => {
        queue_contract_tests!(@cases $open;
            acknowledge_empties_in_progress,
            retry_returns_the_message_to_the_back_of_pending_with_the_attempt_counted,
            move_to_failed_keeps_the_attempt_count,
            release_returns_the_message_to_pending_without_counting_the_attempt,
            release_after_a_clear_reports_lost_and_writes_nothing,
            acknowledge_after_a_clear_reports_lost,
            retry_after_a_clear_reports_lost_and_writes_nothing,
            move_to_failed_after_a_clear_reports_lost_and_writes_nothing,
            a_delivery_from_the_other_lane_is_rejected_and_changes_nothing,
            a_malformed_message_moves_to_failed_and_dequeue_returns_the_next_one,
            an_unreadable_message_moves_to_failed_and_dequeue_returns_the_next_one,
            recover_returns_in_progress_messages_with_the_oldest_claim_first,
            peek_returns_the_requested_range_oldest_first,
            peek_of_a_reversed_range_returns_nothing,
            peek_of_the_largest_range_returns_every_message,
            republish_moves_the_first_failed_messages_to_pending,
            clear_removes_every_message_in_the_stage_and_reports_the_count,
            dequeue_with_a_wait_returns_none_after_the_wait,
            dequeue_with_the_largest_wait_returns_an_available_message
        );
    };
    (@cases $open:expr; $($case:ident),* $(,)?) => {
        $(
            #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
            async fn $case() {
                let fixture = $open.await;
                common::contract::$case(&fixture).await;
            }
        )*
    };
}
