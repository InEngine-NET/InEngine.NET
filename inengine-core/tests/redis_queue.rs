//! Tests the Redis queue driver against the shared queue contract.
//!
//! These tests need a Redis server, version 6.2 or later. Run them with
//! `INENGINE_TEST_REDIS_PORT=<port> cargo test --workspace --features inengine-core/integration`.
#![cfg(feature = "integration")]

#[macro_use]
mod common;

use common::{QueueFixture, claim, expected_lengths, publish_echo, settings_with_queue_base};
use inengine_core::config::{QueueDriver, QueueSettings};
use inengine_core::queue::{Queue, QueueLane, SettleError, Stage};

struct RedisFixture {
    name: String,
    queue: Queue,
    client: redis::Client,
}

impl RedisFixture {
    async fn new() -> Self {
        let name = format!("Test{}", uuid::Uuid::new_v4().simple());
        let queue = Queue::open(QueueLane::Primary, &settings(&name))
            .await
            .expect("open; is Redis running on INENGINE_TEST_REDIS_PORT?");
        let client =
            redis::Client::open(format!("redis://127.0.0.1:{}/", redis_port())).expect("client");
        Self {
            name,
            queue,
            client,
        }
    }

    fn pending_key(&self) -> String {
        format!("{}:Primary:{}", self.name, Stage::Pending.as_str())
    }

    fn push_pending(&self, element: &[u8]) {
        let mut connection = self.client.get_connection().expect("connection");
        redis::cmd("LPUSH")
            .arg(self.pending_key())
            .arg(element)
            .query::<()>(&mut connection)
            .expect("push");
    }
}

impl Drop for RedisFixture {
    fn drop(&mut self) {
        let name = &self.name;
        let keys: Vec<String> = QueueLane::ALL
            .iter()
            .flat_map(|lane| {
                Stage::ALL
                    .iter()
                    .map(move |stage| format!("{name}:{}:{}", lane.as_str(), stage.as_str()))
            })
            .collect();
        if let Ok(mut connection) = self.client.get_connection() {
            let _: Result<(), _> = redis::cmd("DEL").arg(keys).query(&mut connection);
        }
    }
}

impl QueueFixture for RedisFixture {
    fn queue(&self) -> &Queue {
        &self.queue
    }

    async fn open_other_lane(&self) -> Queue {
        Queue::open(QueueLane::Secondary, &settings(&self.name))
            .await
            .expect("open")
    }

    fn insert_malformed_pending(&self, raw: &[u8]) {
        self.push_pending(raw);
    }

    fn insert_unreadable_pending(&self) {
        self.push_pending(&[0xff, 0xfe, 0x00]);
    }
}

fn redis_port() -> u16 {
    std::env::var("INENGINE_TEST_REDIS_PORT")
        .ok()
        .and_then(|port| port.parse().ok())
        .unwrap_or(6379)
}

fn settings(name: &str) -> QueueSettings {
    let mut settings = common::default_settings().queue;
    settings.queue_driver = QueueDriver::Redis;
    settings.queue_name = name.to_string();
    settings.redis.port = redis_port();
    settings
}

queue_contract_tests!(RedisFixture::new());

#[tokio::test]
async fn a_delivery_from_a_file_queue_is_rejected_and_changes_nothing() {
    // Arrange
    let fixture = RedisFixture::new().await;
    let dir = tempfile::tempdir().expect("temporary directory");
    let file_queue = Queue::open(
        QueueLane::Primary,
        &settings_with_queue_base(dir.path()).queue,
    )
    .await
    .expect("open");
    publish_echo(&file_queue, "one", 0).await;
    publish_echo(&fixture.queue, "two", 0).await;
    drop(claim(&fixture.queue).await);
    let file_delivery = claim(&file_queue).await;

    // Act
    let result = fixture.queue.acknowledge(file_delivery).await;

    // Assert
    assert!(
        matches!(result, Err(SettleError::Rejected { .. })),
        "{result:?}"
    );
    assert_eq!(
        fixture.queue.lengths().await.expect("lengths"),
        expected_lengths(0, 1, 0)
    );
    assert_eq!(
        file_queue.lengths().await.expect("lengths"),
        expected_lengths(0, 1, 0)
    );
}
