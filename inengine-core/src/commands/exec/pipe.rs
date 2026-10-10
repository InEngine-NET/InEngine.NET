use std::collections::VecDeque;
use std::io::{self, Read};
use std::sync::mpsc::{self, Receiver, TryRecvError};
use std::sync::{Arc, Mutex, MutexGuard, PoisonError};

const KEPT_HEAD_BYTES: usize = 512 * 1024;
const KEPT_TAIL_BYTES: usize = 512 * 1024;

/// The start and the end of one output stream, and how many bytes between them were dropped.
#[derive(Debug, Default)]
pub(super) struct KeptOutput {
    head: Vec<u8>,
    dropped: u64,
    tail: Vec<u8>,
}

impl KeptOutput {
    /// The kept bytes as text. When bytes were dropped, a line between the head and the tail
    /// says how many.
    pub(super) fn into_text(self) -> String {
        if self.dropped == 0 {
            let mut bytes = self.head;
            bytes.extend_from_slice(&self.tail);
            return into_string(bytes);
        }
        let mut text = into_string(self.head);
        if !text.is_empty() && !text.ends_with('\n') {
            text.push('\n');
        }
        text.push_str(&format!("[{} bytes not kept]\n", self.dropped));
        text.push_str(&into_string(self.tail));
        text
    }
}

fn into_string(bytes: Vec<u8>) -> String {
    String::from_utf8(bytes)
        .unwrap_or_else(|error| String::from_utf8_lossy(error.as_bytes()).into_owned())
}

/// The bytes that a reader thread keeps for Exec. After a take, the thread still drains the
/// pipe, because a background process can hold it open, but it keeps nothing more.
struct StreamBuffer {
    head_limit: usize,
    tail_limit: usize,
    head: Vec<u8>,
    tail: VecDeque<u8>,
    dropped: u64,
    taken: bool,
}

impl StreamBuffer {
    fn new(head_limit: usize, tail_limit: usize) -> Self {
        Self {
            head_limit,
            tail_limit,
            head: Vec::new(),
            tail: VecDeque::new(),
            dropped: 0,
            taken: false,
        }
    }

    fn push(&mut self, bytes: &[u8]) {
        if self.taken {
            return;
        }
        let room = self.head_limit - self.head.len();
        let (to_head, rest) = bytes.split_at(room.min(bytes.len()));
        self.head.extend_from_slice(to_head);
        self.tail.extend(rest);
        let excess = self.tail.len().saturating_sub(self.tail_limit);
        self.tail.drain(..excess);
        self.dropped += excess as u64;
    }

    fn take(&mut self) -> KeptOutput {
        self.taken = true;
        KeptOutput {
            head: std::mem::take(&mut self.head),
            dropped: self.dropped,
            tail: std::mem::take(&mut self.tail).into(),
        }
    }
}

/// One output pipe, read on its own thread into a buffer that the caller can take at any time.
pub(super) struct PipeReader {
    buffer: Arc<Mutex<StreamBuffer>>,
    finished: Receiver<io::Result<()>>,
    done: bool,
}

impl PipeReader {
    pub(super) fn spawn(mut pipe: impl Read + Send + 'static) -> Self {
        let buffer = Arc::new(Mutex::new(StreamBuffer::new(
            KEPT_HEAD_BYTES,
            KEPT_TAIL_BYTES,
        )));
        let (sender, finished) = mpsc::channel();
        let shared = Arc::clone(&buffer);
        std::thread::spawn(move || {
            let mut chunk = [0u8; 8192];
            let read = loop {
                match pipe.read(&mut chunk) {
                    Ok(0) => break Ok(()),
                    Ok(count) => lock(&shared).push(&chunk[..count]),
                    Err(error) if error.kind() == io::ErrorKind::Interrupted => {}
                    Err(error) => break Err(error),
                }
            };
            let _ = sender.send(read);
        });
        Self {
            buffer,
            finished,
            done: false,
        }
    }

    /// Returns true once the pipe has been read to the end.
    pub(super) fn poll(&mut self) -> io::Result<bool> {
        if self.done {
            return Ok(true);
        }
        match self.finished.try_recv() {
            Ok(read) => {
                read?;
                self.done = true;
                Ok(true)
            }
            Err(TryRecvError::Empty) => Ok(false),
            Err(TryRecvError::Disconnected) => Err(io::Error::other("the reader thread stopped")),
        }
    }

    /// The output kept so far. The reader keeps nothing after this call.
    pub(super) fn take(self) -> KeptOutput {
        lock(&self.buffer).take()
    }
}

/// A reader thread holds the lock only to add bytes, so a poisoned lock still holds valid bytes.
fn lock(buffer: &Mutex<StreamBuffer>) -> MutexGuard<'_, StreamBuffer> {
    buffer.lock().unwrap_or_else(PoisonError::into_inner)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_stream_buffer_keeps_the_head_and_the_tail_and_counts_the_rest() {
        // Arrange
        let mut buffer = StreamBuffer::new(3, 2);

        // Act
        buffer.push(b"abcdefgh");

        // Assert
        let kept = buffer.take();
        assert_eq!(kept.head, b"abc");
        assert_eq!(kept.tail, b"gh");
        assert_eq!(kept.dropped, 3);
        assert_eq!(kept.into_text(), "abc\n[3 bytes not kept]\ngh");
    }

    #[test]
    fn a_stream_buffer_discards_bytes_after_a_take() {
        // Arrange
        let mut buffer = StreamBuffer::new(3, 2);
        buffer.push(b"abcdefgh");
        let kept = buffer.take();

        // Act
        buffer.push(b"more");

        // Assert
        assert_eq!(kept.dropped, 3);
        assert!(buffer.head.is_empty());
        assert!(buffer.tail.is_empty());
        assert_eq!(buffer.dropped, 3);
    }

    #[test]
    fn output_within_the_limits_is_kept_whole() {
        // Arrange
        let mut buffer = StreamBuffer::new(3, 4);
        buffer.push("ab\u{e9}cd".as_bytes());

        // Act
        let text = buffer.take().into_text();

        // Assert
        assert_eq!(text, "ab\u{e9}cd");
    }

    struct FailingPipe;

    impl Read for FailingPipe {
        fn read(&mut self, _buf: &mut [u8]) -> io::Result<usize> {
            Err(io::Error::other("broken pipe"))
        }
    }

    fn poll_until_finished(
        reader: &mut PipeReader,
        deadline: std::time::Instant,
    ) -> io::Result<bool> {
        loop {
            let polled = reader.poll();
            if !matches!(polled, Ok(false)) || std::time::Instant::now() >= deadline {
                return polled;
            }
            std::thread::sleep(std::time::Duration::from_millis(5));
        }
    }

    #[test]
    fn a_pipe_read_error_is_returned_by_poll() {
        // Arrange
        let mut reader = PipeReader::spawn(FailingPipe);
        let deadline = std::time::Instant::now() + std::time::Duration::from_secs(5);

        // Act
        let polled = poll_until_finished(&mut reader, deadline);

        // Assert
        let error = polled.expect_err("the read fails");
        assert_eq!(error.to_string(), "broken pipe");
    }
}
