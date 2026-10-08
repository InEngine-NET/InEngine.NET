use std::collections::HashMap;

use crate::command::Command;
use crate::commands::echo::Echo;
use crate::commands::exec::Exec;
use crate::error::QueueError;

type Constructor = Box<dyn Fn(&str) -> Result<Box<dyn Command>, QueueError> + Send + Sync>;

pub struct CommandRegistry {
    constructors: HashMap<String, Constructor>,
}

impl CommandRegistry {
    pub fn new() -> Self {
        let mut registry = Self {
            constructors: HashMap::new(),
        };
        registry.register_builtins();
        registry
    }

    pub fn register<F>(&mut self, command_name: &str, constructor: F)
    where
        F: Fn(&str) -> Result<Box<dyn Command>, QueueError> + Send + Sync + 'static,
    {
        self.constructors
            .insert(command_name.to_string(), Box::new(constructor));
    }

    pub fn build(&self, command_name: &str, json: &str) -> Result<Box<dyn Command>, QueueError> {
        let constructor =
            self.constructors
                .get(command_name)
                .ok_or_else(|| QueueError::UnknownCommand {
                    name: command_name.to_string(),
                })?;
        constructor(json)
    }

    fn register_builtins(&mut self) {
        self.register("inengine_core::commands::echo::Echo", |json| {
            let cmd: Echo = serde_json::from_str(json)?;
            Ok(Box::new(cmd))
        });
        self.register("inengine_core::commands::exec::Exec", |json| {
            let cmd: Exec = serde_json::from_str(json)?;
            Ok(Box::new(cmd))
        });
    }
}

impl Default for CommandRegistry {
    fn default() -> Self {
        Self::new()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn build_echo_command() {
        // Arrange
        let registry = CommandRegistry::new();
        let json = r#"{"text":"hello"}"#;

        // Act
        let cmd = registry
            .build("inengine_core::commands::echo::Echo", json)
            .unwrap();

        // Assert
        assert_eq!(cmd.name(), "echo");
    }

    #[test]
    fn unregistered_name_returns_unknown_command() {
        // Arrange
        let registry = CommandRegistry::new();

        // Act
        let result = registry.build("nonexistent::Command", r#"{}"#);

        // Assert
        let err = result.err().expect("expected an error");
        assert!(matches!(err, QueueError::UnknownCommand { .. }));
    }
}
