use std::io::{self, Read};
use std::process::{Child, ExitStatus, Stdio};
use std::sync::mpsc::{self, Receiver, TryRecvError};
use std::sync::{Arc, Mutex, MutexGuard, PoisonError};
use std::time::{Duration, Instant};

use serde::{Deserialize, Serialize};
use tracing::warn;

use crate::command::{instant_after, Command, CommandContext, CommandResult, QueueableCommand};
use crate::error::CommandError;

const WAIT_POLL_INTERVAL: Duration = Duration::from_millis(10);
/// How long Exec keeps reading after the child exits. A process that the child started in the
/// background can hold a pipe open, and Exec does not wait for that process.
const PIPE_DRAIN_LIMIT: Duration = Duration::from_secs(1);

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Exec {
    /// The name of the executable in `exec_whitelist`.
    pub executable: String,
    #[serde(default)]
    pub arguments: Vec<String>,
    /// Seconds before the process is killed. Zero means no limit.
    pub timeout: u64,
}

impl QueueableCommand for Exec {
    const PLUGIN_NAME: &'static str = crate::BUILTIN_PLUGIN_NAME;
    const COMMAND_NAME: &'static str = "exec";
}

impl Command for Exec {
    fn name(&self) -> &str {
        Self::COMMAND_NAME
    }

    /// Stops the process at its own timeout, at the context deadline, or when the context's
    /// cancel token fires. Only the direct child is killed, so a process it started keeps
    /// running, and Exec returns at most one second after the child exits.
    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let path = ctx
            .settings
            .exec_whitelist
            .get(&self.executable)
            .ok_or_else(|| {
                CommandError::failed(format!(
                    "executable is not whitelisted: {}",
                    self.executable
                ))
            })?
            .clone();

        let output = self.run_process(&path, ctx)?;

        ctx.output.text(&String::from_utf8_lossy(&output.stdout));
        ctx.output.text(&String::from_utf8_lossy(&output.stderr));
        if output.status.success() {
            Ok(())
        } else {
            Err(CommandError::failed(describe_failure(&path, output.status)))
        }
    }
}

fn describe_failure(path: &str, status: ExitStatus) -> String {
    use std::os::unix::process::ExitStatusExt;

    match (status.code(), status.signal()) {
        (Some(code), _) => format!("{path} exited with code {code}"),
        (None, Some(signal)) => format!("{path} was stopped by signal {signal}"),
        (None, None) => format!("{path} ended with {status}"),
    }
}

struct ProcessOutput {
    status: ExitStatus,
    stdout: Vec<u8>,
    stderr: Vec<u8>,
}

/// The earlier of the process timeout and the context deadline.
struct WaitLimit {
    at: Instant,
    /// The timeout reported when the limit passes.
    seconds: u64,
}

/// One output pipe, read on its own thread into a buffer that the caller can take at any time.
struct PipeReader {
    bytes: Arc<Mutex<Vec<u8>>>,
    finished: Receiver<io::Result<()>>,
    done: bool,
}

impl PipeReader {
    fn spawn(mut pipe: impl Read + Send + 'static) -> Self {
        let bytes = Arc::new(Mutex::new(Vec::new()));
        let (sender, finished) = mpsc::channel();
        let buffer = Arc::clone(&bytes);
        std::thread::spawn(move || {
            let mut chunk = [0u8; 8192];
            let read = loop {
                match pipe.read(&mut chunk) {
                    Ok(0) => break Ok(()),
                    Ok(count) => lock(&buffer).extend_from_slice(&chunk[..count]),
                    Err(error) if error.kind() == io::ErrorKind::Interrupted => {}
                    Err(error) => break Err(error),
                }
            };
            let _ = sender.send(read);
        });
        Self {
            bytes,
            finished,
            done: false,
        }
    }

    /// Returns true once the pipe has been read to the end.
    fn poll(&mut self) -> io::Result<bool> {
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

    /// The bytes read so far.
    fn take_bytes(self) -> Vec<u8> {
        std::mem::take(&mut *lock(&self.bytes))
    }
}

/// A reader thread holds the lock only to append, so a poisoned lock still holds valid bytes.
fn lock(bytes: &Mutex<Vec<u8>>) -> MutexGuard<'_, Vec<u8>> {
    bytes.lock().unwrap_or_else(PoisonError::into_inner)
}

impl Exec {
    fn wait_limit(&self, ctx: &CommandContext) -> Option<WaitLimit> {
        let process = (self.timeout > 0).then(|| WaitLimit {
            at: instant_after(Duration::from_secs(self.timeout)),
            seconds: self.timeout,
        });
        let context = ctx.deadline.map(|deadline| WaitLimit {
            at: deadline.at,
            seconds: deadline.timeout.as_secs(),
        });
        match (process, context) {
            (Some(process), Some(context)) if context.at < process.at => Some(context),
            (Some(process), _) => Some(process),
            (None, context) => context,
        }
    }

    fn run_process(&self, path: &str, ctx: &CommandContext) -> Result<ProcessOutput, CommandError> {
        let mut child = std::process::Command::new(path)
            .args(&self.arguments)
            .stdin(Stdio::null())
            .stdout(Stdio::piped())
            .stderr(Stdio::piped())
            .spawn()
            .map_err(|error| match error.kind() {
                io::ErrorKind::NotFound | io::ErrorKind::PermissionDenied => CommandError::failed(
                    format!("cannot run {path}: it does not exist or is inaccessible"),
                ),
                _ => CommandError::failed_with(format!("failed to spawn {path}"), error),
            })?;
        let mut readers = [
            child.stdout.take().map(PipeReader::spawn),
            child.stderr.take().map(PipeReader::spawn),
        ];
        let mut exit: Option<(ExitStatus, Instant)> = None;
        let waited = self.wait_for_exit(&mut child, &mut readers, &mut exit, path, ctx);
        if waited.is_err() {
            stop_child(&mut child, exit.is_some(), path);
        }
        let status = waited?;
        let [stdout, stderr] =
            readers.map(|reader| reader.map(PipeReader::take_bytes).unwrap_or_default());
        Ok(ProcessOutput {
            status,
            stdout,
            stderr,
        })
    }

    /// Waits until the child has exited and its pipes are read, or until a background process
    /// has held a pipe open for `PIPE_DRAIN_LIMIT` after the exit.
    fn wait_for_exit(
        &self,
        child: &mut Child,
        readers: &mut [Option<PipeReader>; 2],
        exit: &mut Option<(ExitStatus, Instant)>,
        path: &str,
        ctx: &CommandContext,
    ) -> Result<ExitStatus, CommandError> {
        let limit = self.wait_limit(ctx);
        loop {
            if exit.is_none() {
                *exit = child
                    .try_wait()
                    .map_err(|error| {
                        CommandError::failed_with(format!("error waiting for {path}"), error)
                    })?
                    .map(|status| (status, Instant::now()));
            }
            let mut drained = true;
            for reader in readers.iter_mut().flatten() {
                drained &= reader.poll().map_err(|error| {
                    CommandError::failed_with(format!("could not read the output of {path}"), error)
                })?;
            }
            if let Some((status, exited_at)) = exit {
                if drained || exited_at.elapsed() >= PIPE_DRAIN_LIMIT {
                    return Ok(*status);
                }
            }
            if ctx.cancel.is_cancelled() {
                return Err(CommandError::Interrupted);
            }
            if let Some(limit) = limit.as_ref().filter(|limit| Instant::now() >= limit.at) {
                return Err(CommandError::Timeout {
                    seconds: limit.seconds,
                });
            }
            std::thread::sleep(WAIT_POLL_INTERVAL);
        }
    }
}

/// Kills and reaps the child unless it has already exited.
fn stop_child(child: &mut Child, exited: bool, path: &str) {
    if exited {
        return;
    }
    if let Err(error) = child.kill() {
        warn!("could not stop {path}: {error}");
    }
    let _ = child.wait();
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::command::CommandDeadline;
    use crate::commands::test_helpers::make_ctx_with_whitelist;
    use std::collections::HashMap;

    #[test]
    fn exec_whitelisted_existing_executable_runs_ok() {
        // Arrange
        let mut whitelist = HashMap::new();
        whitelist.insert("truecmd".to_string(), "/usr/bin/true".to_string());
        let mut ctx = make_ctx_with_whitelist(whitelist);
        let cmd = Exec {
            executable: "truecmd".to_string(),
            arguments: Vec::new(),
            timeout: 10,
        };

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok());
    }

    #[test]
    fn exec_non_whitelisted_returns_failed() {
        // Arrange
        let mut ctx = make_ctx_with_whitelist(HashMap::new());
        let cmd = Exec {
            executable: "notlisted".to_string(),
            arguments: Vec::new(),
            timeout: 10,
        };

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_err());
        let err = result.unwrap_err();
        assert!(
            matches!(err, CommandError::Failed { .. }),
            "expected Failed, got: {err}"
        );
        assert!(err.to_string().contains("not whitelisted"));
    }

    #[test]
    fn exec_whitelisted_but_missing_path_returns_failed() {
        // Arrange
        let mut whitelist = HashMap::new();
        whitelist.insert(
            "missing".to_string(),
            "/nonexistent/path/to/binary".to_string(),
        );
        let mut ctx = make_ctx_with_whitelist(whitelist);
        let cmd = Exec {
            executable: "missing".to_string(),
            arguments: Vec::new(),
            timeout: 10,
        };

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_err());
        let err = result.unwrap_err();
        assert!(
            matches!(err, CommandError::Failed { .. }),
            "expected Failed, got: {err}"
        );
        assert!(err.to_string().contains("does not exist"));
    }

    #[test]
    fn exec_timeout_returns_timeout_error() {
        // Arrange
        let mut whitelist = HashMap::new();
        whitelist.insert("sleeper".to_string(), "/bin/sleep".to_string());
        let mut ctx = make_ctx_with_whitelist(whitelist);
        let cmd = Exec {
            executable: "sleeper".to_string(),
            arguments: vec!["60".to_string()],
            timeout: 1,
        };

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_err());
        let err = result.unwrap_err();
        assert!(
            matches!(err, CommandError::Timeout { seconds: 1 }),
            "expected Timeout, got: {err}"
        );
    }

    fn shell(arguments: &[&str], timeout: u64) -> (Exec, CommandContext) {
        let mut whitelist = HashMap::new();
        whitelist.insert("sh".to_string(), "/bin/sh".to_string());
        let exec = Exec {
            executable: "sh".to_string(),
            arguments: arguments
                .iter()
                .map(|argument| argument.to_string())
                .collect(),
            timeout,
        };
        (exec, make_ctx_with_whitelist(whitelist))
    }

    #[test]
    fn exec_non_zero_exit_returns_failed_after_writing_output() {
        // Arrange
        let (cmd, mut ctx) = shell(&["-c", "echo partial; exit 3"], 10);

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        let err = result.expect_err("a non-zero exit fails");
        assert!(matches!(err, CommandError::Failed { .. }), "got: {err}");
        assert_eq!(
            err.to_string(),
            "command failed: /bin/sh exited with code 3"
        );
        assert_eq!(ctx.output.take_buffer(), "partial\n");
    }

    #[test]
    fn exec_captures_stdout_then_stderr() {
        // Arrange
        let (cmd, mut ctx) = shell(&["-c", "echo to-stderr >&2; echo to-stdout"], 10);

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok(), "{result:?}");
        assert_eq!(ctx.output.take_buffer(), "to-stdout\nto-stderr\n");
    }

    #[test]
    fn exec_passes_an_argument_with_a_space_as_one_argument() {
        // Arrange
        let (cmd, mut ctx) = shell(&["-c", "printf '%s|' \"$@\"", "sh", "a b", "c"], 10);

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok(), "{result:?}");
        assert_eq!(ctx.output.take_buffer(), "a b|c|");
    }

    #[test]
    fn exec_stops_when_the_server_stops() {
        // Arrange
        let mut whitelist = HashMap::new();
        whitelist.insert("sleeper".to_string(), "/bin/sleep".to_string());
        let cancel = tokio_util::sync::CancellationToken::new();
        cancel.cancel();
        let mut ctx = make_ctx_with_whitelist(whitelist).with_cancel(cancel);
        let cmd = Exec {
            executable: "sleeper".to_string(),
            arguments: vec!["60".to_string()],
            timeout: 0,
        };
        let started = Instant::now();

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(started.elapsed() < Duration::from_secs(1));
        assert!(
            matches!(result, Err(CommandError::Interrupted)),
            "{result:?}"
        );
    }

    #[test]
    fn exec_stops_at_the_context_deadline() {
        // Arrange
        let (cmd, ctx) = shell(&["-c", "sleep 60"], 0);
        let mut ctx = ctx.with_deadline(CommandDeadline::after(Duration::from_secs(1)));
        let started = Instant::now();

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(started.elapsed() < Duration::from_secs(3));
        assert!(
            matches!(result, Err(CommandError::Timeout { seconds: 1 })),
            "{result:?}"
        );
    }

    #[test]
    fn exec_returns_when_the_child_exits_while_a_background_process_holds_its_output() {
        // Arrange
        let (cmd, mut ctx) = shell(&["-c", "sleep 5 & echo started"], 0);
        let started = Instant::now();

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(started.elapsed() < Duration::from_secs(3));
        assert!(result.is_ok(), "{result:?}");
        assert_eq!(ctx.output.take_buffer(), "started\n");
    }

    struct FailingPipe;

    impl Read for FailingPipe {
        fn read(&mut self, _buf: &mut [u8]) -> io::Result<usize> {
            Err(io::Error::other("broken pipe"))
        }
    }

    #[test]
    fn a_pipe_read_error_reaches_the_wait_loop() {
        // Arrange
        let mut reader = PipeReader::spawn(FailingPipe);
        std::thread::sleep(Duration::from_millis(100));

        // Act
        let polled = reader.poll();

        // Assert
        let error = polled.expect_err("the read fails");
        assert_eq!(error.to_string(), "broken pipe");
    }

    #[test]
    fn exec_with_a_huge_timeout_does_not_panic() {
        // Arrange
        let (cmd, mut ctx) = shell(&["-c", "echo done"], u64::MAX);

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok(), "{result:?}");
        assert_eq!(ctx.output.take_buffer(), "done\n");
    }

    #[test]
    fn exec_timeout_of_zero_means_no_limit() {
        // Arrange
        let (cmd, mut ctx) = shell(&["-c", "sleep 1; echo done"], 0);

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok(), "{result:?}");
        assert_eq!(ctx.output.take_buffer(), "done\n");
    }
}
