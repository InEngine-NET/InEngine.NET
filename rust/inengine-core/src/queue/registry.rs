use std::collections::HashMap;

use crate::command::{Command, QueueableCommand};
use crate::commands::echo::Echo;
use crate::commands::exec::Exec;
use crate::error::{DuplicateCommand, QueueError};

type Constructor = Box<dyn Fn(&str) -> Result<Box<dyn Command>, QueueError> + Send + Sync>;

/// Builds a queued command from its plugin name, its command name, and its JSON payload.
pub struct CommandRegistry {
    plugins: HashMap<String, HashMap<String, Constructor>>,
}

impl CommandRegistry {
    /// Holds the built-in commands.
    pub fn new() -> Self {
        let mut registry = Self {
            plugins: HashMap::new(),
        };
        registry
            .register_queueable::<Echo>()
            .and_then(|()| registry.register_queueable::<Exec>())
            .expect("the built-in command names are distinct");
        registry
    }

    /// Adds the constructor for a plugin name and command name pair. A second registration of
    /// the pair fails, and the first constructor stays.
    pub fn register<F>(
        &mut self,
        plugin_name: &str,
        command_name: &str,
        constructor: F,
    ) -> Result<(), DuplicateCommand>
    where
        F: Fn(&str) -> Result<Box<dyn Command>, QueueError> + Send + Sync + 'static,
    {
        let commands = self.plugins.entry(plugin_name.to_string()).or_default();
        if commands.contains_key(command_name) {
            return Err(DuplicateCommand {
                plugin: plugin_name.to_string(),
                command: command_name.to_string(),
            });
        }
        commands.insert(command_name.to_string(), Box::new(constructor));
        Ok(())
    }

    /// Adds a constructor that deserializes `T` from the payload, under the names `T` declares.
    pub fn register_queueable<T: QueueableCommand>(&mut self) -> Result<(), DuplicateCommand> {
        self.register(T::PLUGIN_NAME, T::COMMAND_NAME, |json| {
            let command: T = serde_json::from_str(json)?;
            Ok(Box::new(command))
        })
    }

    /// Builds the command for a queued message from its names and its JSON payload.
    pub fn build(
        &self,
        plugin_name: &str,
        command_name: &str,
        json: &str,
    ) -> Result<Box<dyn Command>, QueueError> {
        let constructor = self
            .plugins
            .get(plugin_name)
            .and_then(|commands| commands.get(command_name))
            .ok_or_else(|| QueueError::UnknownCommand {
                plugin: plugin_name.to_string(),
                command: command_name.to_string(),
            })?;
        constructor(json)
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
    use crate::commands::test_helpers::make_ctx;
    use crate::queue::CommandEnvelope;
    use crate::BUILTIN_PLUGIN_NAME;

    struct NamedCommand(&'static str);

    impl Command for NamedCommand {
        fn name(&self) -> &str {
            self.0
        }

        fn run(&self, _ctx: &mut crate::command::CommandContext) -> crate::command::CommandResult {
            Ok(())
        }
    }

    fn unknown_command_names(error: QueueError) -> (String, String) {
        match error {
            QueueError::UnknownCommand { plugin, command } => (plugin, command),
            other => panic!("expected UnknownCommand, got {other:?}"),
        }
    }

    #[test]
    fn message_published_with_builtin_plugin_and_command_name_runs() {
        // Arrange
        let registry = CommandRegistry::new();
        let envelope =
            CommandEnvelope::new("echo", "inengine-core", r#"{"text":"hello"}"#, false, 0).unwrap();
        let mut ctx = make_ctx();

        // Act
        let command = envelope.to_command(&registry).expect("echo resolves");

        // Assert
        assert_eq!(command.name(), "echo");
        command.run(&mut ctx).expect("echo runs");
        assert_eq!(ctx.output.take_buffer(), "hello\n");
    }

    #[test]
    fn same_command_name_under_two_plugins_resolves_to_each() {
        // Arrange
        let mut registry = CommandRegistry::new();
        registry
            .register("first", "run", |_| Ok(Box::new(NamedCommand("first-run"))))
            .unwrap();
        registry
            .register("second", "run", |_| {
                Ok(Box::new(NamedCommand("second-run")))
            })
            .unwrap();

        // Act
        let names = ["first", "second"].map(|plugin| registry.build(plugin, "run", "{}"));

        // Assert
        let names = names.map(|built| built.expect("registered").name().to_string());
        assert_eq!(names, ["first-run", "second-run"]);
    }

    #[test]
    fn unknown_plugin_returns_unknown_command_with_both_names() {
        // Arrange
        let registry = CommandRegistry::new();

        // Act
        let result = registry.build("missing-plugin", "echo", "{}");

        // Assert
        let names = unknown_command_names(result.err().expect("an error"));
        assert_eq!(names, ("missing-plugin".to_string(), "echo".to_string()));
    }

    #[test]
    fn unknown_command_returns_unknown_command_with_both_names() {
        // Arrange
        let registry = CommandRegistry::new();

        // Act
        let result = registry.build(BUILTIN_PLUGIN_NAME, "missing", "{}");

        // Assert
        let error = result.err().expect("an error");
        assert_eq!(
            error.to_string(),
            "command not found in plugin 'inengine-core': missing"
        );
        let names = unknown_command_names(error);
        assert_eq!(names, ("inengine-core".to_string(), "missing".to_string()));
    }

    #[test]
    fn duplicate_registration_fails_and_keeps_the_first() {
        // Arrange
        let mut registry = CommandRegistry::new();
        registry
            .register("plugin", "run", |_| Ok(Box::new(NamedCommand("first"))))
            .unwrap();

        // Act
        let result = registry.register("plugin", "run", |_| Ok(Box::new(NamedCommand("second"))));

        // Assert
        let duplicate = result.expect_err("the pair is already registered");
        assert_eq!(
            duplicate.to_string(),
            "duplicate command in plugin 'plugin': run"
        );
        let kept = registry.build("plugin", "run", "{}").unwrap();
        assert_eq!(kept.name(), "first");
    }

    #[test]
    fn builtin_commands_resolve_under_the_builtin_plugin() {
        // Arrange
        let registry = CommandRegistry::new();

        // Act
        let echo = registry.build(BUILTIN_PLUGIN_NAME, "echo", r#"{"text":"hi"}"#);
        let exec = registry.build(
            BUILTIN_PLUGIN_NAME,
            "exec",
            r#"{"executable":"hello","timeout":1}"#,
        );

        // Assert
        assert_eq!(echo.unwrap().name(), "echo");
        assert_eq!(exec.unwrap().name(), "exec");
    }
}
