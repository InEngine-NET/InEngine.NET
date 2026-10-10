mod pipe;

use std::io;
use std::path::Path;
use std::process::{Child, ExitStatus, Stdio};
use std::time::{Duration, Instant};

use serde::{Deserialize, Serialize};
use tracing::warn;

use crate::command::{Command, CommandContext, CommandResult, QueueableCommand, instant_after};
use crate::error::{CommandError, ErrorChain};
use pipe::{KeptOutput, PipeReader};

const WAIT_POLL_INTERVAL: Duration = Duration::from_millis(10);
/// How long Exec keeps reading after the child exits. A process that the child started in the
/// background can hold a pipe open, and Exec does not wait for that process.
const PIPE_DRAIN_LIMIT: Duration = Duration::from_secs(1);

/// Runs an executable from `exec_whitelist` and writes its standard output, then its standard
/// error, to the command output. Exec keeps the first 512 KiB and the last 512 KiB of each
/// stream, and writes a line between them that says how many bytes it did not keep. A nonzero
/// exit code or a stop by a signal fails the command.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Exec {
    /// The name of the executable in `exec_whitelist`.
    pub executable: String,
    /// The arguments given to the process, in order. A missing value means no arguments.
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

        let run = self.run_process(&path, ctx)?;

        ctx.output.text(&run.stdout.into_text());
        ctx.output.text(&run.stderr.into_text());
        let status = run.status?;
        if status.success() {
            Ok(())
        } else {
            Err(CommandError::failed(describe_failure(&path, status)))
        }
    }
}

fn describe_failure(path: &Path, status: ExitStatus) -> String {
    use std::os::unix::process::ExitStatusExt;

    match (status.code(), status.signal()) {
        (Some(code), _) => format!("{} exited with code {code}", path.display()),
        (None, Some(signal)) => format!("{} was stopped by signal {signal}", path.display()),
        (None, None) => format!("{} ended with {status}", path.display()),
    }
}

/// How the process ended, and the output that Exec kept, which it writes even after an error.
struct ProcessRun {
    status: Result<ExitStatus, CommandError>,
    stdout: KeptOutput,
    stderr: KeptOutput,
}

/// The earlier of the process timeout and the context deadline.
struct WaitLimit {
    at: Instant,
    /// The timeout reported when the limit passes.
    seconds: u64,
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

    fn run_process(&self, path: &Path, ctx: &CommandContext) -> Result<ProcessRun, CommandError> {
        let mut child = std::process::Command::new(path)
            .args(&self.arguments)
            .stdin(Stdio::null())
            .stdout(Stdio::piped())
            .stderr(Stdio::piped())
            .spawn()
            .map_err(|error| match error.kind() {
                io::ErrorKind::NotFound | io::ErrorKind::PermissionDenied => {
                    CommandError::failed(format!(
                        "could not run {}: it does not exist or is not accessible",
                        path.display()
                    ))
                }
                _ => {
                    CommandError::failed_with(format!("could not start {}", path.display()), error)
                }
            })?;
        let mut readers = [
            child.stdout.take().map(PipeReader::spawn),
            child.stderr.take().map(PipeReader::spawn),
        ];
        let status = self.wait_for_exit(&mut child, &mut readers, path, ctx);
        let [stdout, stderr] =
            readers.map(|reader| reader.map(PipeReader::take).unwrap_or_default());
        Ok(ProcessRun {
            status,
            stdout,
            stderr,
        })
    }

    /// Waits until the child has exited and its pipes are read, or until a background process
    /// has held a pipe open for `PIPE_DRAIN_LIMIT` after the exit. On an error, the child is
    /// stopped unless it has already exited.
    fn wait_for_exit(
        &self,
        child: &mut Child,
        readers: &mut [Option<PipeReader>; 2],
        path: &Path,
        ctx: &CommandContext,
    ) -> Result<ExitStatus, CommandError> {
        let limit = self.wait_limit(ctx);
        let mut exit: Option<(ExitStatus, Instant)> = None;
        let waited = loop {
            if exit.is_none() {
                match child.try_wait() {
                    Ok(status) => exit = status.map(|status| (status, Instant::now())),
                    Err(error) => {
                        break Err(CommandError::failed_with(
                            format!("could not wait for {}", path.display()),
                            error,
                        ));
                    }
                }
            }
            let polled: io::Result<Vec<bool>> =
                readers.iter_mut().flatten().map(PipeReader::poll).collect();
            let drained = match polled {
                Ok(polled) => polled.into_iter().all(|done| done),
                Err(error) => {
                    break Err(CommandError::failed_with(
                        format!("could not read the output of {}", path.display()),
                        error,
                    ));
                }
            };
            if let Some((status, exited_at)) = exit
                && (drained || exited_at.elapsed() >= PIPE_DRAIN_LIMIT)
            {
                break Ok(status);
            }
            if ctx.cancel.is_cancelled() {
                break Err(CommandError::Interrupted);
            }
            if let Some(limit) = limit.as_ref().filter(|limit| Instant::now() >= limit.at) {
                break Err(CommandError::Timeout {
                    seconds: limit.seconds,
                });
            }
            std::thread::sleep(WAIT_POLL_INTERVAL);
        };
        if waited.is_err() {
            stop_child(child, exit.is_some(), path);
        }
        waited
    }
}

/// Kills and reaps the child unless it has already exited.
fn stop_child(child: &mut Child, exited: bool, path: &Path) {
    if exited {
        return;
    }
    if let Err(error) = child.kill() {
        warn!(
            "could not stop {}: {}",
            path.display(),
            ErrorChain::new(&error)
        );
    }
    let _ = child.wait();
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::command::CommandDeadline;
    use crate::commands::test_helpers::make_ctx_with_whitelist;
    use std::collections::HashMap;
    use std::path::PathBuf;

    #[test]
    fn exec_whitelisted_existing_executable_runs_ok() {
        // Arrange
        let mut whitelist = HashMap::new();
        whitelist.insert("truecmd".to_string(), PathBuf::from("/usr/bin/true"));
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
            PathBuf::from("/nonexistent/path/to/binary"),
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
        whitelist.insert("sleeper".to_string(), PathBuf::from("/bin/sleep"));
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
        whitelist.insert("sh".to_string(), PathBuf::from("/bin/sh"));
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
    fn exec_keeps_the_head_and_tail_of_a_large_output() {
        // Arrange
        let script = "printf start; head -c 2097152 /dev/zero | tr '\\0' a; printf end";
        let (cmd, mut ctx) = shell(&["-c", script], 30);

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok(), "{result:?}");
        let output = ctx.output.take_buffer();
        assert!(output.len() <= 1024 * 1024 + 64, "{} bytes", output.len());
        assert!(output.starts_with("start"));
        assert!(output.ends_with("end"));
    }

    #[test]
    fn exec_limits_standard_error_the_same_way() {
        // Arrange
        let script = "head -c 2097152 /dev/zero | tr '\\0' b >&2";
        let (cmd, mut ctx) = shell(&["-c", script], 30);

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok(), "{result:?}");
        let output = ctx.output.take_buffer();
        let lines: Vec<&str> = output.lines().collect();
        assert_eq!(lines.len(), 3);
        assert_eq!(lines[0].len(), 512 * 1024);
        assert_eq!(lines[1], "[1048576 bytes not kept]");
        assert_eq!(lines[2].len(), 512 * 1024);
    }

    #[test]
    fn exec_that_times_out_keeps_the_output_it_read() {
        // Arrange
        let (cmd, mut ctx) = shell(&["-c", "echo partial; sleep 10"], 1);

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(
            matches!(result, Err(CommandError::Timeout { seconds: 1 })),
            "{result:?}"
        );
        assert_eq!(ctx.output.take_buffer(), "partial\n");
    }

    #[test]
    fn exec_reports_how_many_bytes_it_dropped() {
        // Arrange
        let script = "printf start; head -c 2097152 /dev/zero | tr '\\0' a; printf end";
        let (cmd, mut ctx) = shell(&["-c", script], 30);

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok(), "{result:?}");
        let output = ctx.output.take_buffer();
        let lines: Vec<&str> = output.lines().collect();
        assert_eq!(lines.len(), 3);
        assert_eq!(lines[0].len(), 512 * 1024);
        assert_eq!(lines[1], "[1048584 bytes not kept]");
        assert_eq!(lines[2].len(), 512 * 1024);
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
        whitelist.insert("sleeper".to_string(), PathBuf::from("/bin/sleep"));
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
