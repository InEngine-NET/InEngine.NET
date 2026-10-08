use serde::{Deserialize, Serialize};

use crate::command::{Command, CommandContext, CommandResult};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Echo {
    pub text: String,
}

impl Command for Echo {
    fn name(&self) -> &str {
        "echo"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        ctx.output.line(&self.text);
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::commands::test_helpers::make_ctx;

    #[test]
    fn echo_hello_writes_hello_with_newline() {
        // Arrange
        let cmd = Echo {
            text: "hello".to_string(),
        };
        let mut ctx = make_ctx();

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok());
        assert_eq!(ctx.output.take_buffer(), "hello\n");
    }

    #[test]
    fn echo_empty_text_writes_newline() {
        // Arrange
        let cmd = Echo {
            text: String::new(),
        };
        let mut ctx = make_ctx();

        // Act
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok());
        assert_eq!(ctx.output.take_buffer(), "\n");
    }
}
