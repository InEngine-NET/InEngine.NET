use std::io;
use std::process::{ExitStatus, Stdio};
use std::time::Duration;

use serde::{Deserialize, Serialize};

use crate::command::{Command, CommandContext, CommandResult};
use crate::error::CommandError;

/// Runs a whitelisted executable.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Exec {
    /// The name of the executable in `exec_whitelist`.
    pub executable: String,
    /// Arguments passed to the executable, each as one argument.
    #[serde(default)]
    pub arguments: Vec<String>,
    /// Seconds before the process is killed. Zero means no limit.
    pub timeout: u64,
}

impl Command for Exec {
    fn name(&self) -> &str {
        "exec"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let path = ctx
            .settings
            .exec_whitelist
            .get(&self.executable)
            .ok_or_else(|| CommandError::Failed {
                message: format!("executable is not whitelisted: {}", self.executable),
            })?
            .clone();

        let output = tokio::task::block_in_place(|| {
            tokio::runtime::Handle::current().block_on(self.run_process(&path))
        })?;

        ctx.output.text(&String::from_utf8_lossy(&output.stdout));
        ctx.output.text(&String::from_utf8_lossy(&output.stderr));
        if output.status.success() {
            Ok(())
        } else {
            Err(CommandError::Failed {
                message: describe_failure(&path, output.status),
            })
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

impl Exec {
    async fn run_process(&self, path: &str) -> Result<std::process::Output, CommandError> {
        let child = tokio::process::Command::new(path)
            .args(&self.arguments)
            .stdin(Stdio::null())
            .stdout(Stdio::piped())
            .stderr(Stdio::piped())
            .kill_on_drop(true)
            .spawn()
            .map_err(|error| match error.kind() {
                io::ErrorKind::NotFound | io::ErrorKind::PermissionDenied => CommandError::Failed {
                    message: format!("cannot run {path}: it does not exist or is inaccessible"),
                },
                _ => CommandError::FailedWithSource {
                    message: format!("failed to spawn {path}"),
                    source: Box::new(error),
                },
            })?;
        let waited = if self.timeout == 0 {
            child.wait_with_output().await
        } else {
            tokio::time::timeout(Duration::from_secs(self.timeout), child.wait_with_output())
                .await
                .map_err(|_elapsed| CommandError::Timeout {
                    seconds: self.timeout,
                })?
        };
        waited.map_err(|error| CommandError::FailedWithSource {
            message: format!("error waiting for {path}"),
            source: Box::new(error),
        })
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::commands::test_helpers::make_ctx_with_whitelist;
    use std::collections::HashMap;

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn exec_whitelisted_existing_executable_runs_ok() {
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

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn exec_whitelisted_but_missing_path_returns_failed() {
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

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn exec_timeout_returns_timeout_error() {
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

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn exec_non_zero_exit_returns_failed_after_writing_output() {
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
        assert_eq!(ctx.output.flush_buffer(), "partial\n");
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn exec_captures_stdout_then_stderr() {
        // Arrange
        let (cmd, mut ctx) = shell(&["-c", "echo to-stderr >&2; echo to-stdout"], 10);

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok(), "{result:?}");
        assert_eq!(ctx.output.flush_buffer(), "to-stdout\nto-stderr\n");
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn exec_passes_an_argument_with_a_space_as_one_argument() {
        // Arrange
        let (cmd, mut ctx) = shell(&["-c", "printf '%s|' \"$@\"", "sh", "a b", "c"], 10);

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok(), "{result:?}");
        assert_eq!(ctx.output.flush_buffer(), "a b|c|");
    }

    #[tokio::test(flavor = "multi_thread", worker_threads = 2)]
    async fn exec_timeout_of_zero_means_no_limit() {
        // Arrange
        let (cmd, mut ctx) = shell(&["-c", "sleep 1; echo done"], 0);

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok(), "{result:?}");
        assert_eq!(ctx.output.flush_buffer(), "done\n");
    }
}
