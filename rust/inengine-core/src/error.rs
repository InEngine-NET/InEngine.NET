#[derive(Debug, thiserror::Error)]
pub enum CommandError {
    #[error("command failed: {message}")]
    Failed { message: String },

    #[error("command failed: {message}")]
    FailedWithSource {
        message: String,
        #[source]
        source: Box<dyn std::error::Error + Send + Sync>,
    },

    #[error("command timed out after {seconds} seconds")]
    Timeout { seconds: u64 },
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

    #[error("command not found in registry: {type_name}")]
    CommandNotFound { type_name: String },

    #[error("queue error: {message}")]
    Other { message: String },
}

#[derive(Debug, thiserror::Error)]
pub enum PluginError {
    #[error("failed to load plugin '{name}': {reason}")]
    LoadFailed { name: String, reason: String },

    #[error("command not found in plugin '{plugin}': {command}")]
    CommandNotFound { plugin: String, command: String },

    #[error("plugin runtime error in '{plugin}': {message}")]
    RuntimeError { plugin: String, message: String },
}
