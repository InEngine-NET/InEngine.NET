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

    pub fn register<F>(&mut self, type_name: &str, constructor: F)
    where
        F: Fn(&str) -> Result<Box<dyn Command>, QueueError> + Send + Sync + 'static,
    {
        self.constructors
            .insert(type_name.to_string(), Box::new(constructor));
    }

    pub fn deserialize_command(
        &self,
        type_name: &str,
        json: &str,
    ) -> Result<Box<dyn Command>, QueueError> {
        let constructor = self.constructors.get(type_name).ok_or_else(|| {
            QueueError::CommandNotFound {
                type_name: type_name.to_string(),
            }
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
    fn deserialize_echo_command() {
        // Arrange
        let registry = CommandRegistry::new();
        let json = r#"{"text":"hello"}"#;

        // Act
        let cmd = registry
            .deserialize_command("inengine_core::commands::echo::Echo", json)
            .unwrap();

        // Assert
        assert_eq!(cmd.name(), "echo");
    }

    #[test]
    fn unregistered_type_returns_command_not_found() {
        // Arrange
        let registry = CommandRegistry::new();

        // Act
        let result =
            registry.deserialize_command("nonexistent::Command", r#"{}"#);

        // Assert
        let err = result.err().expect("expected an error");
        assert!(matches!(err, QueueError::CommandNotFound { .. }));
    }
}
