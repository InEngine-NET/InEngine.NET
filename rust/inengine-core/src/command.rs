use std::sync::Arc;

use crate::config::Settings;
use crate::error::CommandError;
use crate::output::Output;

pub type CommandResult = Result<(), CommandError>;

pub trait Command: Send + Sync {
    fn name(&self) -> &str;
    fn run(&self, ctx: &mut CommandContext) -> CommandResult;
}

pub struct CommandContext {
    pub output: Output,
    pub settings: Arc<Settings>,
}

#[cfg(test)]
mod tests {
    use super::*;

    struct TestCommand;

    impl Command for TestCommand {
        fn name(&self) -> &str {
            "test"
        }

        fn run(&self, ctx: &mut CommandContext) -> CommandResult {
            ctx.output.info("test ran");
            Ok(())
        }
    }

    #[test]
    fn command_can_be_called_via_run() {
        // Arrange
        let settings = crate::config::from_spec(&crate::config::SettingsSpec::default())
            .settings
            .expect("the default settings are valid");
        let settings = Arc::new(settings);
        let mut ctx = CommandContext {
            output: Output::new(false),
            settings,
        };
        let cmd = TestCommand;

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok());
        assert_eq!(cmd.name(), "test");
        let buf = ctx.output.flush_buffer();
        assert!(buf.contains("test ran"));
    }

    #[test]
    fn command_error_failed_carries_message() {
        // Arrange
        let error = CommandError::Failed {
            message: "something broke".to_string(),
        };

        // Act
        let display = format!("{error}");

        // Assert
        assert_eq!(display, "command failed: something broke");
    }

    #[test]
    fn command_error_timeout_carries_seconds() {
        // Arrange
        let error = CommandError::Timeout { seconds: 30 };

        // Act
        let display = format!("{error}");

        // Assert
        assert_eq!(display, "command timed out after 30 seconds");
    }
}
