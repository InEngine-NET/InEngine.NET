use crate::command::{Command, CommandContext, CommandResult};

/// Runs each command in order with one shared context. The first error stops the chain and is
/// returned, so the later commands do not run.
pub struct Chain {
    /// The commands to run, first to last.
    pub commands: Vec<Box<dyn Command>>,
}

impl Command for Chain {
    fn name(&self) -> &str {
        "chain"
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        for cmd in &self.commands {
            cmd.run(ctx)?;
        }
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::commands::test_helpers::make_ctx;
    use crate::error::CommandError;

    struct SucceedCommand {
        label: String,
    }

    impl Command for SucceedCommand {
        fn name(&self) -> &str {
            &self.label
        }

        fn run(&self, ctx: &mut CommandContext) -> CommandResult {
            ctx.output.line(&self.label);
            Ok(())
        }
    }

    struct FailCommand;

    impl Command for FailCommand {
        fn name(&self) -> &str {
            "fail"
        }

        fn run(&self, _ctx: &mut CommandContext) -> CommandResult {
            Err(CommandError::failed("intentional failure"))
        }
    }

    #[test]
    fn chain_of_two_succeeding_commands_runs_both() {
        // Arrange
        let chain = Chain {
            commands: vec![
                Box::new(SucceedCommand {
                    label: "first".to_string(),
                }),
                Box::new(SucceedCommand {
                    label: "second".to_string(),
                }),
            ],
        };
        let mut ctx = make_ctx();

        // Act
        let result = chain.run(&mut ctx);

        // Assert
        assert!(result.is_ok());
        let buf = ctx.output.take_buffer();
        assert!(buf.contains("first"));
        assert!(buf.contains("second"));
    }

    #[test]
    fn chain_stops_on_first_failure() {
        // Arrange
        let chain = Chain {
            commands: vec![
                Box::new(SucceedCommand {
                    label: "first".to_string(),
                }),
                Box::new(FailCommand),
                Box::new(SucceedCommand {
                    label: "third".to_string(),
                }),
            ],
        };
        let mut ctx = make_ctx();

        // Act
        let result = chain.run(&mut ctx);

        // Assert
        assert!(result.is_err());
        let buf = ctx.output.take_buffer();
        assert!(buf.contains("first"));
        assert!(!buf.contains("third"));
    }

    #[test]
    fn empty_chain_returns_ok() {
        // Arrange
        let chain = Chain { commands: vec![] };
        let mut ctx = make_ctx();

        // Act
        let result = chain.run(&mut ctx);

        // Assert
        assert!(result.is_ok());
    }
}
