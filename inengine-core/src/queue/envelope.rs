use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};
use uuid::Uuid;

use crate::command::{Command, QueueableCommand};
use crate::error::QueueError;
use crate::queue::CommandRegistry;
use crate::queue::compression::{compress, decompress};

/// A queued command with the data a consumer needs to build and run it. The queue stores it
/// as JSON.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CommandEnvelope {
    /// A time ordered UUID (version 7) made when you create the envelope. It stays the same
    /// through retries and republishing.
    pub id: Uuid,
    /// The plugin that provides the command. Built-in commands use
    /// [`BUILTIN_PLUGIN_NAME`](crate::BUILTIN_PLUGIN_NAME).
    pub plugin_name: String,
    /// The command name within its plugin. With `plugin_name` it selects the constructor in
    /// the [`CommandRegistry`].
    pub command_name: String,
    /// The command arguments as JSON. When `is_compressed` is true, this holds the JSON
    /// compressed with gzip and encoded as base64.
    pub payload: String,
    /// The time, in UTC, when the envelope was created.
    pub queued_at: DateTime<Utc>,
    /// Whether `payload` is compressed. A consumer decompresses it before it builds the
    /// command.
    pub is_compressed: bool,
    /// The number of attempts to run the message. Each claim adds one, and a retry or a move to
    /// failed stores the new count. An attempt that the server interrupts, or one that a crash
    /// stops before you recover the queue, does not count.
    pub attempts: u32,
    /// The number of retries after the first failed attempt. Zero means the first failure
    /// moves the message to the failed stage.
    pub max_retries: u32,
}

impl CommandEnvelope {
    /// Creates an envelope with a new id, the current time, and zero attempts. `json` is the
    /// command arguments, and it is compressed when `use_compression` is true. Fails only when
    /// compression fails.
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
            id: Uuid::now_v7(),
            plugin_name: plugin_name.to_string(),
            command_name: command_name.to_string(),
            payload: serialized,
            queued_at: Utc::now(),
            is_compressed: use_compression,
            attempts: 0,
            max_retries,
        })
    }

    /// Creates an envelope from a command value. The command type gives the plugin name and
    /// command name, and the value is serialized to JSON as the payload.
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

    pub(crate) fn to_command(
        &self,
        registry: &CommandRegistry,
    ) -> Result<Box<dyn Command>, QueueError> {
        let json = if self.is_compressed {
            decompress(&self.payload)?
        } else {
            self.payload.clone()
        };
        registry.build(&self.plugin_name, &self.command_name, &json)
    }

    pub(crate) fn has_retries_left(&self) -> bool {
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
        let envelope = CommandEnvelope::new("missing", "unknown", "{}", false, 0).unwrap();
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
        let mut envelope = CommandEnvelope::new("test", "test", "{}", false, 3).unwrap();
        envelope.attempts = 1;

        // Act
        let has_retries_left = envelope.has_retries_left();

        // Assert
        assert!(has_retries_left);
    }

    #[test]
    fn has_retries_left_is_false_when_exhausted() {
        // Arrange
        let mut envelope = CommandEnvelope::new("test", "test", "{}", false, 3).unwrap();
        envelope.attempts = 4;

        // Act
        let has_retries_left = envelope.has_retries_left();

        // Assert
        assert!(!has_retries_left);
    }

    #[test]
    fn an_envelope_id_serializes_as_a_hyphenated_string() {
        // Arrange
        let envelope = CommandEnvelope::new("echo", "inengine-core", "{}", false, 0).unwrap();

        // Act
        let json: serde_json::Value = serde_json::to_value(&envelope).unwrap();

        // Assert
        let id = json["id"].as_str().expect("the id is a string");
        assert_eq!(id, envelope.id.hyphenated().to_string());
        assert_eq!(id.len(), 36);
    }

    #[test]
    fn an_envelope_with_a_hyphenated_string_id_parses() {
        // Arrange
        let json = r#"{
            "id": "0199c9a4-6f1e-7c3a-9b2d-4e5f6a7b8c9d",
            "plugin_name": "inengine-core",
            "command_name": "echo",
            "payload": "{}",
            "queued_at": "2026-10-09T12:00:00Z",
            "is_compressed": false,
            "attempts": 2,
            "max_retries": 3
        }"#;

        // Act
        let envelope: CommandEnvelope = serde_json::from_str(json).unwrap();

        // Assert
        assert_eq!(
            envelope.id,
            Uuid::parse_str("0199c9a4-6f1e-7c3a-9b2d-4e5f6a7b8c9d").unwrap()
        );
        assert_eq!(envelope.plugin_name, "inengine-core");
        assert_eq!(envelope.command_name, "echo");
        assert_eq!(envelope.attempts, 2);
        assert_eq!(envelope.max_retries, 3);
    }

    #[test]
    fn an_envelope_with_an_id_that_is_not_a_uuid_does_not_parse() {
        // Arrange
        let json = r#"{"id":"test-id","plugin_name":"p","command_name":"c","payload":"{}",
            "queued_at":"2026-10-09T12:00:00Z","is_compressed":false,"attempts":0,"max_retries":0}"#;

        // Act
        let parsed = serde_json::from_str::<CommandEnvelope>(json);

        // Assert
        let error = parsed.expect_err("the id is not a UUID");
        assert!(error.is_data(), "{error}");
    }
}
