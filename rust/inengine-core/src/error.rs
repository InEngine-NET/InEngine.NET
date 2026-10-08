type BoxedError = Box<dyn std::error::Error + Send + Sync>;

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

    #[error("command not found in registry: {name}")]
    UnknownCommand { name: String },
}

#[derive(Debug, thiserror::Error)]
pub enum PluginError {
    #[error("failed to load plugin '{name}': {reason}")]
    Load { name: String, reason: String },

    #[error("command not found in plugin '{plugin}': {command}")]
    CommandNotFound { plugin: String, command: String },

    #[error("plugin runtime error in '{plugin}': {message}")]
    Runtime { plugin: String, message: String },
}
