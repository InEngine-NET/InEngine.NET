use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};

use crate::command::{Command, QueueableCommand};
use crate::error::QueueError;
use crate::queue::compression::{compress, decompress};
use crate::queue::registry::CommandRegistry;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CommandEnvelope {
    pub id: String,
    pub plugin_name: String,
    pub command_name: String,
    pub payload: String,
    pub queued_at: DateTime<Utc>,
    pub is_compressed: bool,
    pub attempts: u32,
    pub max_retries: u32,
}

impl CommandEnvelope {
    pub fn new(
        command_name: &str,
        plugin_name: &str,
        json: &str,
        use_compression: bool,
        max_retries: u32,
    ) -> Result<Self, QueueError> {
        let serialized = if use_compression {
            compress(json)?
        } else {
            json.to_string()
        };
        Ok(Self {
            id: uuid::Uuid::now_v7().to_string(),
            plugin_name: plugin_name.to_string(),
            command_name: command_name.to_string(),
            payload: serialized,
            queued_at: Utc::now(),
            is_compressed: use_compression,
            attempts: 0,
            max_retries,
        })
    }

    pub fn from_command<T: QueueableCommand>(
        cmd: &T,
        use_compression: bool,
        max_retries: u32,
    ) -> Result<Self, QueueError> {
        let json = serde_json::to_string(cmd)?;
        Self::new(
            T::COMMAND_NAME,
            T::PLUGIN_NAME,
            &json,
            use_compression,
            max_retries,
        )
    }

    pub fn to_command(&self, registry: &CommandRegistry) -> Result<Box<dyn Command>, QueueError> {
        let json = if self.is_compressed {
            decompress(&self.payload)?
        } else {
            self.payload.clone()
        };
        registry.build(&self.plugin_name, &self.command_name, &json)
    }

    pub fn has_retries_left(&self) -> bool {
        self.attempts <= self.max_retries
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::commands::echo::Echo;
    use crate::commands::exec::Exec;
    use crate::commands::test_helpers::make_ctx;

    #[test]
    fn roundtrip_echo_command() {
        // Arrange
        let echo = Echo {
            text: "hello".to_string(),
        };
        let registry = CommandRegistry::new();

        // Act
        let envelope = CommandEnvelope::from_command(&echo, false, 0).unwrap();
        let cmd = envelope.to_command(&registry).unwrap();
        let mut ctx = make_ctx();
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok());
        assert_eq!(ctx.output.take_buffer(), "hello\n");
    }

    #[test]
    fn from_command_writes_the_declared_plugin_and_command_names() {
        // Arrange
        let echo = Echo {
            text: "hello".to_string(),
        };

        // Act
        let envelope = CommandEnvelope::from_command(&echo, false, 0).unwrap();

        // Assert
        assert_eq!(envelope.plugin_name, "inengine-core");
        assert_eq!(envelope.command_name, "echo");
        assert_eq!(envelope.payload, r#"{"text":"hello"}"#);
    }

    #[test]
    fn roundtrip_with_compression() {
        // Arrange
        let echo = Echo {
            text: "compressed payload".to_string(),
        };
        let registry = CommandRegistry::new();

        // Act
        let envelope = CommandEnvelope::from_command(&echo, true, 0).unwrap();

        // Assert
        assert!(envelope.is_compressed);
        assert_ne!(envelope.payload, r#"{"text":"compressed payload"}"#);

        // Act
        let cmd = envelope.to_command(&registry).unwrap();
        let mut ctx = make_ctx();
        let result = cmd.run(&mut ctx);

        // Assert
        assert!(result.is_ok());
        assert_eq!(ctx.output.take_buffer(), "compressed payload\n");
    }

    #[test]
    fn roundtrip_exec_command() {
        // Arrange
        let exec = Exec {
            executable: "test".to_string(),
            arguments: vec!["--help".to_string()],
            timeout: 30,
        };
        let registry = CommandRegistry::new();

        // Act
        let envelope = CommandEnvelope::from_command(&exec, false, 0).unwrap();
        let cmd = envelope.to_command(&registry).unwrap();

        // Assert
        assert_eq!(cmd.name(), "exec");
    }

    #[test]
    fn unregistered_type_returns_error() {
        // Arrange
        let envelope = CommandEnvelope {
            id: "test-id".to_string(),
            plugin_name: "unknown".to_string(),
            command_name: "missing".to_string(),
            payload: "{}".to_string(),
            queued_at: Utc::now(),
            is_compressed: false,
            attempts: 0,
            max_retries: 0,
        };
        let registry = CommandRegistry::new();

        // Act
        let result = envelope.to_command(&registry);

        // Assert
        let err = result.err().expect("expected an error");
        assert!(matches!(
            err,
            QueueError::UnknownCommand { ref plugin, ref command }
                if plugin == "unknown" && command == "missing"
        ));
    }

    #[test]
    fn has_retries_left_is_true_when_retries_remain() {
        // Arrange
        let envelope = CommandEnvelope {
            id: "test".to_string(),
            plugin_name: "test".to_string(),
            command_name: "test".to_string(),
            payload: "{}".to_string(),
            queued_at: Utc::now(),
            is_compressed: false,
            attempts: 1,
            max_retries: 3,
        };

        // Act / Assert
        assert!(envelope.has_retries_left());
    }

    #[test]
    fn has_retries_left_is_false_when_exhausted() {
        // Arrange
        let envelope = CommandEnvelope {
            id: "test".to_string(),
            plugin_name: "test".to_string(),
            command_name: "test".to_string(),
            payload: "{}".to_string(),
            queued_at: Utc::now(),
            is_compressed: false,
            attempts: 4,
            max_retries: 3,
        };

        // Act / Assert
        assert!(!envelope.has_retries_left());
    }
}
