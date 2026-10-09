type BoxedError = Box<dyn std::error::Error + Send + Sync>;

/// `error` on a line that starts with "Error: ", then one "  caused by: " line for each source.
pub fn describe_error_chain(error: &dyn std::error::Error) -> String {
    use std::fmt::Write;

    let mut text = format!("Error: {error}\n");
    let mut source = error.source();
    while let Some(cause) = source {
        let _ = writeln!(text, "  caused by: {cause}");
        source = cause.source();
    }
    text
}

#[derive(Debug, thiserror::Error)]
pub enum CommandError {
    #[error("command failed: {message}")]
    Failed {
        message: String,
        #[source]
        source: Option<BoxedError>,
    },

    #[error("command timed out after {seconds} seconds")]
    Timeout { seconds: u64 },

    /// The server stopped while the command ran, so the command did not finish or fail.
    #[error("command stopped because the server is stopping")]
    Interrupted,
}

impl CommandError {
    pub fn failed(message: impl Into<String>) -> Self {
        Self::Failed {
            message: message.into(),
            source: None,
        }
    }

    pub fn failed_with(message: impl Into<String>, source: impl Into<BoxedError>) -> Self {
        Self::Failed {
            message: message.into(),
            source: Some(source.into()),
        }
    }
}

#[derive(Debug, thiserror::Error)]
pub enum ConfigError {
    #[error("configuration file not found: {path}")]
    FileNotFound { path: String },

    #[error("invalid configuration:\n{diagnostics}")]
    Invalid { diagnostics: String },

    #[error("IO error reading configuration: {0}")]
    Io(std::io::Error),
}

#[derive(Debug, thiserror::Error)]
pub enum QueueError {
    #[error("queue serialization error: {0}")]
    Serialization(#[from] serde_json::Error),

    #[error("queue compression error: {message}")]
    Compression { message: String },

    #[error("queue IO error: {0}")]
    Io(#[from] std::io::Error),

    #[error("redis error: {0}")]
    Redis(#[from] redis::RedisError),

    #[error("command not found in plugin '{plugin}': {command}")]
    UnknownCommand { plugin: String, command: String },
}

/// A second registration of a plugin name and command name pair.
#[derive(Debug, thiserror::Error)]
#[error("duplicate command in plugin '{plugin}': {command}")]
pub struct DuplicateCommand {
    pub plugin: String,
    pub command: String,
}

#[derive(Debug, thiserror::Error)]
pub enum PluginError {
    #[error("failed to load plugin '{name}': {reason}")]
    Load { name: String, reason: String },

    #[error(transparent)]
    DuplicateCommand(#[from] DuplicateCommand),

    #[error("invalid schedule entry for command '{command}' in plugin '{plugin}': {reason}")]
    InvalidScheduleEntry {
        plugin: String,
        command: String,
        reason: String,
    },

    #[error("could not start the plugin runtime: {message}")]
    Engine { message: String },
}
