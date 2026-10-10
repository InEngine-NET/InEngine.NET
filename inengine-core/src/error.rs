use std::error::Error;
use std::fmt::{self, Write};
use std::path::PathBuf;

use crate::plugin::{LoadError, ScheduleEntryError};

type BoxedError = Box<dyn Error + Send + Sync>;

/// `error` and its sources. A source whose text ends the text of the error before it is
/// skipped, because some errors, such as `redis::RedisError`, show their source in their own
/// message.
fn chain<'a>(error: &'a (dyn Error + 'static)) -> impl Iterator<Item = &'a (dyn Error + 'static)> {
    let mut previous = String::new();
    std::iter::successors(Some(error), |&error| error.source()).filter(move |error| {
        let text = error.to_string();
        let repeated = !text.is_empty() && previous.ends_with(&text);
        previous = text;
        !repeated
    })
}

/// `error` on a line that starts with "Error: ", then one "  caused by: " line for each source.
pub fn describe_error_chain(error: &(dyn Error + 'static)) -> String {
    let mut errors = chain(error);
    let mut text = String::new();
    if let Some(first) = errors.next() {
        let _ = writeln!(text, "Error: {first}");
    }
    for cause in errors {
        let _ = writeln!(text, "  caused by: {cause}");
    }
    text
}

/// Shows an error and each of its sources on one line, separated by ": ".
#[derive(Debug, Clone, Copy)]
pub struct ErrorChain<'a>(&'a (dyn Error + 'static));

impl<'a> ErrorChain<'a> {
    /// The chain that starts at `error`.
    pub fn new(error: &'a (dyn Error + 'static)) -> Self {
        Self(error)
    }
}

impl fmt::Display for ErrorChain<'_> {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        for (index, error) in chain(self.0).enumerate() {
            if index > 0 {
                f.write_str(": ")?;
            }
            write!(f, "{error}")?;
        }
        Ok(())
    }
}

/// The reason a command run did not succeed. A command returns it from `run`.
#[derive(Debug, thiserror::Error)]
pub enum CommandError {
    /// The command ran and reported a failure, for example a nonzero exit code or a bad payload.
    #[error("command failed: {message}")]
    Failed {
        /// What went wrong, written for a person who reads the log.
        message: String,
        /// The lower level error that caused the failure, if there is one.
        #[source]
        source: Option<BoxedError>,
    },

    /// The command ran past its time limit and was stopped.
    #[error("command timed out after {seconds} seconds")]
    Timeout {
        /// The time limit that the command exceeded, in seconds.
        seconds: u64,
    },

    /// The server stopped while the command ran, so the command did not finish or fail.
    #[error("command stopped because the server is stopping")]
    Interrupted,
}

impl CommandError {
    /// Makes a `Failed` error with no source. Use it when the failure has no lower level error.
    pub fn failed(message: impl Into<String>) -> Self {
        Self::Failed {
            message: message.into(),
            source: None,
        }
    }

    /// Makes a `Failed` error that keeps `source`, so the error chain shows the cause.
    pub fn failed_with(message: impl Into<String>, source: impl Into<BoxedError>) -> Self {
        Self::Failed {
            message: message.into(),
            source: Some(source.into()),
        }
    }
}

/// The reason the configuration file did not give usable settings.
#[derive(Debug, thiserror::Error)]
pub enum ConfigError {
    /// No file exists at the given path.
    #[error("configuration file not found: {}", path.display())]
    FileNotFound {
        /// The path that was searched.
        path: PathBuf,
    },

    /// The file was read, but its contents have one or more errors.
    #[error("invalid configuration:\n{diagnostics}")]
    Invalid {
        /// The rendered report of each problem, as plain text without color.
        diagnostics: String,
    },

    /// The file exists but could not be read, for example because of its permissions.
    #[error("could not read the configuration file {}", path.display())]
    Io {
        /// The path of the file that could not be read.
        path: PathBuf,
        /// The error from the file system.
        #[source]
        source: std::io::Error,
    },
}

/// The reason a queue operation or a message conversion did not complete.
#[derive(Debug, thiserror::Error)]
pub enum QueueError {
    /// A message or its payload could not be converted to or from JSON.
    #[error("could not serialize or parse a queued message")]
    Serialization(#[from] serde_json::Error),

    /// Compression of a payload before it was queued did not complete.
    #[error("could not compress the payload")]
    Compress(#[source] std::io::Error),

    /// A compressed payload could not be expanded, for example because its data is damaged.
    #[error("could not decompress the payload")]
    Decompress(#[source] std::io::Error),

    /// A message is marked as compressed, but its payload text cannot be decoded as base64.
    #[error("the payload is not valid base64")]
    PayloadNotBase64(#[from] base64::DecodeError),

    /// The file queue driver could not read, write, or move a message file.
    #[error("could not read or write the queue files")]
    Io(#[from] std::io::Error),

    /// The Redis queue driver could not connect, or Redis returned an error.
    #[error("could not complete a Redis command")]
    Redis(#[from] redis::RedisError),

    /// A message refers to a plugin and command pair that the registry does not have, for
    /// example because the plugin is not loaded.
    #[error("command not found in plugin '{plugin}': {command}")]
    UnknownCommand {
        /// The plugin name in the message.
        plugin: String,
        /// The command name in the message.
        command: String,
    },
}

/// A second registration of a plugin name and command name pair.
#[derive(Debug, thiserror::Error)]
#[error("duplicate command in plugin '{plugin}': {command}")]
pub struct DuplicateCommand {
    /// The plugin name of the pair that was already registered.
    pub plugin: String,
    /// The command name of the pair that was already registered.
    pub command: String,
}

/// The reason a plugin, or one of its commands or schedule entries, was not made available.
#[derive(Debug, thiserror::Error)]
pub enum PluginError {
    /// A plugin directory or one of its components did not load. The source tells you why.
    #[error("could not load plugin '{name}' from {}", path.display())]
    Load {
        /// The plugin name from the configuration.
        name: String,
        /// The directory or component file that did not load.
        path: PathBuf,
        /// The cause of the failure.
        #[source]
        source: LoadError,
    },

    /// Two components register a command with the same plugin name and command name. The
    /// second one is skipped.
    #[error(transparent)]
    DuplicateCommand(#[from] DuplicateCommand),

    /// A plugin lists a schedule entry that cannot be scheduled. The entry is skipped.
    #[error("invalid schedule entry for command '{command}' in plugin '{plugin}'")]
    InvalidScheduleEntry {
        /// The plugin that lists the entry.
        plugin: String,
        /// The command name in the entry.
        command: String,
        /// The problem with the entry.
        #[source]
        source: ScheduleEntryError,
    },

    /// The WebAssembly engine could not be created from the runtime configuration.
    #[error("could not start the plugin runtime")]
    Engine(#[source] wasmtime::Error),

    /// The operating system could not start the thread that enforces plugin time limits.
    #[error("could not start the plugin time limit thread")]
    Ticker(#[source] std::io::Error),
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn describe_error_chain_shows_each_source_once() {
        // Arrange
        let error = QueueError::Io(std::io::Error::other("the disk is full"));

        // Act
        let text = describe_error_chain(&error);

        // Assert
        assert_eq!(text.matches("the disk is full").count(), 1, "{text}");
        assert!(text.ends_with("  caused by: the disk is full\n"), "{text}");
    }

    #[test]
    fn error_chain_shows_each_source_on_one_line() {
        // Arrange
        let error = CommandError::failed_with(
            "could not publish",
            QueueError::Io(std::io::Error::other("the disk is full")),
        );

        // Act
        let text = ErrorChain::new(&error).to_string();

        // Assert
        assert_eq!(
            text,
            "command failed: could not publish: could not read or write the queue files: \
             the disk is full"
        );
    }

    #[test]
    fn describe_error_chain_skips_a_source_that_its_parent_already_shows() {
        // Arrange
        let refused = std::io::Error::new(std::io::ErrorKind::ConnectionRefused, "refused");
        let error = QueueError::Redis(redis::RedisError::from(refused));

        // Act
        let text = describe_error_chain(&error);

        // Assert
        assert_eq!(text.matches("refused").count(), 1, "{text}");
        assert!(text.ends_with("  caused by: refused\n"), "{text}");
    }
}
