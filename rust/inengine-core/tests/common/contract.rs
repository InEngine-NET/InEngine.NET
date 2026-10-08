use std::time::{Duration, Instant};

use inengine_core::queue::{PeekedMessage, SettleError, Stage};

use super::{claim, expected_lengths, peeked_ids, publish_echo, QueueFixture};

pub async fn acknowledge_empties_in_progress(fixture: &impl QueueFixture) {
    // Arrange
    let queue = fixture.queue();
    publish_echo(queue, "one", 0).await;
    let delivery = claim(queue).await;

    // Act
    let result = queue.acknowledge(delivery).await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 0, 0)
    );
}

pub async fn retry_returns_the_message_to_the_back_of_pending_with_the_attempt_counted(
    fixture: &impl QueueFixture,
) {
    // Arrange
    let queue = fixture.queue();
    let first = publish_echo(queue, "first", 1).await;
    let second = publish_echo(queue, "second", 1).await;
    let delivery = claim(queue).await;

    // Act
    let result = queue.retry(delivery).await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    let next = claim(queue).await;
    let retried = claim(queue).await;
    assert_eq!(next.envelope.id, second);
    assert_eq!(next.envelope.current_try, 1);
    assert_eq!(retried.envelope.id, first);
    assert_eq!(retried.envelope.current_try, 2);
}

pub async fn fail_moves_the_message_to_failed_with_the_attempt_counted(
    fixture: &impl QueueFixture,
) {
    // Arrange
    let queue = fixture.queue();
    let id = publish_echo(queue, "one", 0).await;
    let delivery = claim(queue).await;

    // Act
    let result = queue.fail(delivery).await;

    // Assert
    assert!(result.is_ok(), "{result:?}");
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 0, 1)
    );
    match queue
        .peek(Stage::Failed, 0..=9)
        .await
        .expect("peek")
        .as_slice()
    {
        [PeekedMessage::Envelope(envelope)] => {
            assert_eq!(envelope.id, id);
            assert_eq!(envelope.current_try, 1);
        }
        other => panic!("expected one envelope, got {other:?}"),
    }
}

pub async fn acknowledge_after_a_clear_reports_lost(fixture: &impl QueueFixture) {
    // Arrange
    let queue = fixture.queue();
    publish_echo(queue, "one", 0).await;
    let delivery = claim(queue).await;
    queue.clear(Stage::InProgress).await.expect("clear");

    // Act
    let result = queue.acknowledge(delivery).await;

    // Assert
    assert!(matches!(result, Err(SettleError::Lost)), "{result:?}");
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 0, 0)
    );
}

pub async fn retry_after_a_clear_reports_lost_and_writes_nothing(fixture: &impl QueueFixture) {
    // Arrange
    let queue = fixture.queue();
    publish_echo(queue, "one", 5).await;
    let delivery = claim(queue).await;
    queue.clear(Stage::InProgress).await.expect("clear");

    // Act
    let result = queue.retry(delivery).await;

    // Assert
    assert!(matches!(result, Err(SettleError::Lost)), "{result:?}");
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 0, 0)
    );
}

pub async fn fail_after_a_clear_reports_lost_and_writes_nothing(fixture: &impl QueueFixture) {
    // Arrange
    let queue = fixture.queue();
    publish_echo(queue, "one", 0).await;
    let delivery = claim(queue).await;
    queue.clear(Stage::InProgress).await.expect("clear");

    // Act
    let result = queue.fail(delivery).await;

    // Assert
    assert!(matches!(result, Err(SettleError::Lost)), "{result:?}");
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 0, 0)
    );
}

pub async fn a_delivery_from_the_other_lane_is_rejected_and_changes_nothing(
    fixture: &impl QueueFixture,
) {
    // Arrange
    let queue = fixture.queue();
    let other_lane = fixture.open_other_lane().await;
    publish_echo(queue, "one", 0).await;
    let delivery = claim(queue).await;

    // Act
    let result = other_lane.acknowledge(delivery).await;

    // Assert
    match result {
        Err(SettleError::Rejected { delivery }) => assert_eq!(delivery.lane(), queue.lane()),
        other => panic!("expected a rejected delivery, got {other:?}"),
    }
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 1, 0)
    );
    assert_ne!(other_lane.lane(), queue.lane());
}

pub async fn a_malformed_message_moves_to_failed_and_dequeue_returns_the_next_one(
    fixture: &impl QueueFixture,
) {
    // Arrange
    let queue = fixture.queue();
    fixture.insert_malformed_pending(b"not json");
    let id = publish_echo(queue, "valid", 0).await;

    // Act
    let delivery = claim(queue).await;

    // Assert
    assert_eq!(delivery.envelope.id, id);
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 1, 1)
    );
    match queue
        .peek(Stage::Failed, 0..=9)
        .await
        .expect("peek")
        .as_slice()
    {
        [PeekedMessage::Malformed { raw, reason }] => {
            assert_eq!(raw, "not json");
            assert!(reason.contains("expected"), "{reason}");
        }
        other => panic!("expected one malformed entry, got {other:?}"),
    }
}

pub async fn an_unreadable_message_moves_to_failed_and_dequeue_returns_the_next_one(
    fixture: &impl QueueFixture,
) {
    // Arrange
    let queue = fixture.queue();
    fixture.insert_unreadable_pending();
    let id = publish_echo(queue, "valid", 0).await;

    // Act
    let delivery = claim(queue).await;

    // Assert
    assert_eq!(delivery.envelope.id, id);
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 1, 1)
    );
}

pub async fn recover_returns_in_progress_messages_with_the_oldest_claim_first(
    fixture: &impl QueueFixture,
) {
    // Arrange
    let queue = fixture.queue();
    let first = publish_echo(queue, "first", 0).await;
    let second = publish_echo(queue, "second", 0).await;
    drop(claim(queue).await);
    drop(claim(queue).await);

    // Act
    let recovered = queue.recover().await.expect("recover");

    // Assert
    assert_eq!(recovered, 2);
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(2, 0, 0)
    );
    let pending = queue.peek(Stage::Pending, 0..=9).await.expect("peek");
    assert_eq!(peeked_ids(&pending), vec![first, second]);
}

pub async fn peek_returns_the_requested_range_oldest_first(fixture: &impl QueueFixture) {
    // Arrange
    let queue = fixture.queue();
    let mut ids = Vec::new();
    for text in ["a", "b", "c", "d"] {
        ids.push(publish_echo(queue, text, 0).await);
    }

    // Act
    let peeked = queue.peek(Stage::Pending, 1..=2).await.expect("peek");

    // Assert
    assert_eq!(peeked_ids(&peeked), ids[1..=2].to_vec());
}

pub async fn peek_of_a_reversed_range_returns_nothing(fixture: &impl QueueFixture) {
    // Arrange
    let queue = fixture.queue();
    for text in ["a", "b", "c", "d"] {
        publish_echo(queue, text, 0).await;
    }

    // Act
    #[allow(clippy::reversed_empty_ranges)]
    let peeked = queue.peek(Stage::Pending, 3..=1).await.expect("peek");

    // Assert
    assert!(peeked.is_empty(), "{peeked:?}");
}

pub async fn peek_of_the_largest_range_returns_every_message(fixture: &impl QueueFixture) {
    // Arrange
    let queue = fixture.queue();
    let first = publish_echo(queue, "first", 0).await;
    let second = publish_echo(queue, "second", 0).await;

    // Act
    let peeked = queue
        .peek(Stage::Pending, 0..=usize::MAX)
        .await
        .expect("peek");

    // Assert
    assert_eq!(peeked_ids(&peeked), vec![first, second]);
}

pub async fn republish_moves_the_first_failed_messages_to_pending(fixture: &impl QueueFixture) {
    // Arrange
    let queue = fixture.queue();
    let first = publish_echo(queue, "first", 0).await;
    let second = publish_echo(queue, "second", 0).await;
    for _ in 0..2 {
        let delivery = claim(queue).await;
        queue.fail(delivery).await.expect("fail");
    }

    // Act
    let moved = queue.republish_failed(1).await.expect("republish");

    // Assert
    assert_eq!(moved, 1);
    let pending = queue.peek(Stage::Pending, 0..=9).await.expect("peek");
    let failed = queue.peek(Stage::Failed, 0..=9).await.expect("peek");
    assert_eq!(peeked_ids(&pending), vec![first]);
    assert_eq!(peeked_ids(&failed), vec![second]);
}

pub async fn clear_removes_every_message_in_the_stage_and_reports_the_count(
    fixture: &impl QueueFixture,
) {
    // Arrange
    let queue = fixture.queue();
    for text in ["a", "b", "c"] {
        publish_echo(queue, text, 0).await;
    }
    let delivery = claim(queue).await;
    queue.fail(delivery).await.expect("fail");

    // Act
    let removed = queue.clear(Stage::Pending).await.expect("clear");

    // Assert
    assert_eq!(removed, 2);
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(0, 0, 1)
    );
}

pub async fn dequeue_with_a_wait_returns_none_after_the_wait(fixture: &impl QueueFixture) {
    // Arrange
    let queue = fixture.queue();
    let started = Instant::now();

    // Act
    let result = queue
        .dequeue(Duration::from_millis(300))
        .await
        .expect("dequeue");

    // Assert
    let elapsed = started.elapsed();
    assert!(result.is_none());
    assert!(
        elapsed >= Duration::from_millis(290),
        "returned after {elapsed:?}"
    );
    assert!(
        elapsed < Duration::from_secs(3),
        "returned after {elapsed:?}"
    );
}

pub async fn dequeue_with_the_largest_wait_returns_an_available_message(
    fixture: &impl QueueFixture,
) {
    // Arrange
    let queue = fixture.queue();
    let id = publish_echo(queue, "one", 0).await;

    // Act
    let delivery = queue.dequeue(Duration::MAX).await.expect("dequeue");

    // Assert
    assert_eq!(delivery.expect("a message").envelope.id, id);
}
