use std::path::Path;
use std::time::Duration;

use serde::{Deserialize, Serialize};

use crate::command::{Command, CommandContext, CommandResult};
use crate::error::CommandError;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Exec {
    pub executable: String,
    pub arguments: Option<String>,
    pub timeout: u64,
}

impl Command for Exec {
    fn name(&self) -> &str {
        "exec"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let whitelist = &ctx.settings.exec_whitelist;

        let file_path = whitelist.get(&self.executable).ok_or_else(|| {
            CommandError::Failed {
                message: format!("executable is not whitelisted: {}", self.executable),
            }
        })?;

        if !Path::new(file_path).exists() {
            return Err(CommandError::Failed {
                message: format!(
                    "cannot run {file_path}: it does not exist or is inaccessible"
                ),
            });
        }

        let file_path = file_path.clone();
        let arguments = self.arguments.clone();
        let timeout_secs = self.timeout;

        tokio::task::block_in_place(|| {
            tokio::runtime::Handle::current().block_on(async {
            let mut cmd = tokio::process::Command::new(&file_path);
            if let Some(ref args) = arguments {
                cmd.args(args.split_whitespace());
            }

            let mut child = cmd.spawn().map_err(|e| CommandError::FailedWithSource {
                message: format!("failed to spawn {file_path}"),
                source: Box::new(e),
            })?;

            match tokio::time::timeout(Duration::from_secs(timeout_secs), child.wait()).await {
                Ok(Ok(_status)) => Ok(()),
                Ok(Err(e)) => Err(CommandError::FailedWithSource {
                    message: format!("error waiting for {file_path}"),
                    source: Box::new(e),
                }),
                Err(_elapsed) => {
                    let _ = child.kill().await;
                    Err(CommandError::Timeout {
                        seconds: timeout_secs,
                    })
                }
            }
            })
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
            arguments: None,
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
            arguments: None,
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
            arguments: None,
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
            arguments: Some("60".to_string()),
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
}
