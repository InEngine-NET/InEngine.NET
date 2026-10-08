#[macro_use]
mod common;

use std::collections::HashSet;
use std::ffi::OsString;
use std::fs::{self, File};
use std::os::unix::fs::PermissionsExt;
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use std::time::{Duration, SystemTime};

use common::{
    claim, drain_ids, echo_envelope, expected_lengths, publish_echo, settings_with_queue_base,
    QueueFixture,
};
use inengine_core::queue::{Delivery, Queue, QueueLane, Stage};
use tempfile::TempDir;

struct FileFixture {
    dir: TempDir,
    queue: Queue,
}

impl FileFixture {
    async fn new() -> Self {
        let dir = tempfile::tempdir().expect("temporary directory");
        let queue = open_lane(dir.path(), QueueLane::Primary).await;
        Self { dir, queue }
    }

    fn stage_dir(&self, stage: Stage) -> PathBuf {
        self.dir
            .path()
            .join(format!("InEngineQueue_Primary_{}", stage.as_str()))
    }

    fn names_in(&self, stage: Stage) -> Vec<OsString> {
        let mut names: Vec<_> = fs::read_dir(self.stage_dir(stage))
            .expect("read dir")
            .map(|entry| entry.expect("entry").file_name())
            .collect();
        names.sort();
        names
    }

    async fn claim_with_file_name(&self) -> (Delivery, OsString) {
        let delivery = claim(&self.queue).await;
        let name = self
            .names_in(Stage::InProgress)
            .pop()
            .expect("one in-progress file");
        (delivery, name)
    }
}

impl QueueFixture for FileFixture {
    fn queue(&self) -> &Queue {
        &self.queue
    }

    async fn open_other_lane(&self) -> Queue {
        open_lane(self.dir.path(), QueueLane::Secondary).await
    }

    fn insert_malformed_pending(&self, raw: &[u8]) {
        fs::write(self.stage_dir(Stage::Pending).join("0000malformed"), raw).expect("write");
    }

    fn insert_unreadable_pending(&self) {
        let path = self.stage_dir(Stage::Pending).join("0000unreadable");
        fs::write(&path, "{}").expect("write");
        fs::set_permissions(&path, fs::Permissions::from_mode(0o000)).expect("chmod");
    }
}

async fn open_lane(base: &Path, lane: QueueLane) -> Queue {
    Queue::open(lane, &settings_with_queue_base(base).queue)
        .await
        .expect("open")
}

fn set_modified(path: &Path, time: SystemTime) {
    File::options()
        .write(true)
        .open(path)
        .expect("open file")
        .set_modified(time)
        .expect("set time");
}

fn two_hours_ago() -> SystemTime {
    SystemTime::now() - Duration::from_secs(2 * 60 * 60)
}

queue_contract_tests!(FileFixture::new());

#[tokio::test]
async fn a_hidden_file_in_pending_is_not_counted() {
    // Arrange
    let fixture = FileFixture::new().await;
    fs::write(fixture.stage_dir(Stage::Pending).join(".partial.tmp"), "{").expect("write");

    // Act
    let lengths = fixture.queue.lengths().await.expect("lengths");

    // Assert
    assert_eq!(lengths, expected_lengths(0, 0, 0));
}

#[tokio::test]
async fn a_hidden_file_in_pending_is_not_dequeued() {
    // Arrange
    let fixture = FileFixture::new().await;
    fs::write(fixture.stage_dir(Stage::Pending).join(".partial.tmp"), "{").expect("write");

    // Act
    let dequeued = fixture
        .queue
        .dequeue(Duration::ZERO)
        .await
        .expect("dequeue");

    // Assert
    assert!(dequeued.is_none());
    assert_eq!(
        fixture.names_in(Stage::Pending),
        vec![OsString::from(".partial.tmp")]
    );
}

#[tokio::test]
async fn a_temporary_file_in_progress_is_not_recovered() {
    // Arrange
    let fixture = FileFixture::new().await;
    fs::write(fixture.stage_dir(Stage::InProgress).join(".other.tmp"), "{").expect("write");

    // Act
    let recovered = fixture.queue.recover().await.expect("recover");

    // Assert
    assert_eq!(recovered, 0);
    assert!(fixture.names_in(Stage::Pending).is_empty());
}

#[tokio::test]
async fn recover_returns_a_settling_file_to_pending_under_its_original_name() {
    // Arrange
    let fixture = FileFixture::new().await;
    let contents = serde_json::to_vec(&echo_envelope("interrupted", 0)).expect("json");
    let settling = fixture
        .stage_dir(Stage::InProgress)
        .join(".0000interrupted.settling");
    fs::write(settling, &contents).expect("write");

    // Act
    let recovered = fixture.queue.recover().await.expect("recover");

    // Assert
    assert_eq!(recovered, 1);
    assert_eq!(
        fixture.names_in(Stage::Pending),
        vec![OsString::from("0000interrupted")]
    );
    let restored = fs::read(fixture.stage_dir(Stage::Pending).join("0000interrupted"));
    assert_eq!(restored.expect("read"), contents);
    assert!(fixture.names_in(Stage::InProgress).is_empty());
}

#[tokio::test]
async fn a_settle_continues_from_a_settling_file_left_by_an_earlier_attempt() {
    // Arrange
    let fixture = FileFixture::new().await;
    publish_echo(&fixture.queue, "one", 1).await;
    let (delivery, name) = fixture.claim_with_file_name().await;
    let in_progress = fixture.stage_dir(Stage::InProgress);
    let mut settling = OsString::from(".");
    settling.push(&name);
    settling.push(".settling");
    fs::rename(in_progress.join(&name), in_progress.join(settling)).expect("rename");

    // Act
    let result = fixture.queue.retry(delivery).await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    assert_eq!(
        fixture.queue.lengths().await.expect("lengths"),
        expected_lengths(1, 0, 0)
    );
    assert!(fixture.names_in(Stage::InProgress).is_empty());
}

#[tokio::test]
async fn clear_in_progress_removes_settling_files_and_stale_temporary_files() {
    // Arrange
    let fixture = FileFixture::new().await;
    publish_echo(&fixture.queue, "one", 0).await;
    drop(claim(&fixture.queue).await);
    let in_progress = fixture.stage_dir(Stage::InProgress);
    fs::write(in_progress.join(".0000held.settling"), "{}").expect("write");
    fs::write(in_progress.join(".stale.tmp"), "{").expect("write");
    set_modified(&in_progress.join(".stale.tmp"), two_hours_ago());
    fs::write(in_progress.join(".fresh.tmp"), "{").expect("write");

    // Act
    let removed = fixture.queue.clear(Stage::InProgress).await.expect("clear");

    // Assert
    assert_eq!(removed, 2);
    assert_eq!(
        fixture.names_in(Stage::InProgress),
        vec![OsString::from(".fresh.tmp")]
    );
}

#[tokio::test]
async fn messages_with_equal_modification_times_dequeue_in_publish_order() {
    // Arrange
    let fixture = FileFixture::new().await;
    let mut published = Vec::new();
    for i in 0..20 {
        published.push(publish_echo(&fixture.queue, &i.to_string(), 0).await);
    }
    let same_time = SystemTime::now() - Duration::from_secs(60);
    for name in fixture.names_in(Stage::Pending) {
        set_modified(&fixture.stage_dir(Stage::Pending).join(name), same_time);
    }

    // Act
    let dequeued = drain_ids(&fixture.queue).await;

    // Assert
    assert_eq!(dequeued, published);
}

#[tokio::test(flavor = "multi_thread", worker_threads = 4)]
async fn two_queues_on_one_directory_consume_distinct_messages_without_errors() {
    // Arrange
    let fixture = FileFixture::new().await;
    let mut published = HashSet::new();
    for i in 0..60 {
        published.insert(publish_echo(&fixture.queue, &i.to_string(), 0).await);
    }
    let base = fixture.dir.path().to_path_buf();
    let consume = |base: PathBuf| {
        tokio::spawn(async move { drain_ids(&open_lane(&base, QueueLane::Primary).await).await })
    };

    // Act
    let (left, right) = tokio::join!(consume(base.clone()), consume(base));

    // Assert
    let left = left.expect("left consumer");
    let right = right.expect("right consumer");
    let consumed: HashSet<_> = left.iter().chain(right.iter()).cloned().collect();
    assert_eq!(left.len() + right.len(), published.len());
    assert_eq!(consumed, published);
}

#[tokio::test]
async fn republish_gives_a_failed_message_a_new_name_in_pending() {
    // Arrange
    let fixture = FileFixture::new().await;
    publish_echo(&fixture.queue, "one", 0).await;
    let delivery = claim(&fixture.queue).await;
    fixture.queue.move_to_failed(delivery).await.expect("fail");
    let failed_names = fixture.names_in(Stage::Failed);

    // Act
    let moved = fixture.queue.republish_failed(1).await.expect("republish");

    // Assert
    assert_eq!(moved, 1);
    let pending_names = fixture.names_in(Stage::Pending);
    assert_eq!(pending_names.len(), 1);
    assert_ne!(pending_names, failed_names);
}

#[tokio::test]
async fn open_removes_temporary_files_older_than_one_hour() {
    // Arrange
    let fixture = FileFixture::new().await;
    let pending = fixture.stage_dir(Stage::Pending);
    File::create(pending.join(".old.tmp")).expect("create");
    set_modified(&pending.join(".old.tmp"), two_hours_ago());
    File::create(pending.join(".new.tmp")).expect("create");

    // Act
    drop(open_lane(fixture.dir.path(), QueueLane::Primary).await);

    // Assert
    assert_eq!(
        fixture.names_in(Stage::Pending),
        vec![OsString::from(".new.tmp")]
    );
}

#[tokio::test(flavor = "multi_thread", worker_threads = 4)]
async fn open_succeeds_while_other_processes_publish() {
    // Arrange
    let fixture = FileFixture::new().await;
    let base = fixture.dir.path().to_path_buf();
    let stop = Arc::new(AtomicBool::new(false));
    let publishers: Vec<_> = (0..3)
        .map(|_| {
            let base = base.clone();
            let stop = stop.clone();
            tokio::spawn(async move {
                let queue = open_lane(&base, QueueLane::Primary).await;
                while !stop.load(Ordering::Relaxed) {
                    publish_echo(&queue, "load", 0).await;
                }
            })
        })
        .collect();
    let settings = settings_with_queue_base(&base).queue;

    // Act
    let failures = count_failed_opens(&settings, 2000).await;

    // Assert
    stop.store(true, Ordering::Relaxed);
    for publisher in publishers {
        publisher.await.expect("publisher");
    }
    assert_eq!(failures, 0);
}

async fn count_failed_opens(
    settings: &inengine_core::config::QueueSettings,
    times: usize,
) -> usize {
    let mut failures = 0;
    for _ in 0..times {
        if Queue::open(QueueLane::Primary, settings).await.is_err() {
            failures += 1;
        }
    }
    failures
}
