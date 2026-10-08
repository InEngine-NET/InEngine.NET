use std::collections::HashMap;

use serde::Deserialize;

use crate::error::ConfigError;

#[derive(Debug, Clone, Deserialize)]
pub struct Settings {
    #[serde(default)]
    pub plugins: HashMap<String, String>,
    #[serde(default)]
    pub exec_whitelist: HashMap<String, String>,
    #[serde(default)]
    pub mail: MailSettings,
    #[serde(default)]
    pub queue: QueueSettings,
}

impl Settings {
    pub fn load(path: &str) -> Result<Settings, ConfigError> {
        let content = std::fs::read_to_string(path).map_err(|e| {
            if e.kind() == std::io::ErrorKind::NotFound {
                ConfigError::FileNotFound {
                    path: path.to_string(),
                }
            } else {
                ConfigError::Io(e)
            }
        })?;
        let settings: Settings = toml::from_str(&content)?;
        Ok(settings)
    }
}

#[derive(Debug, Clone, Deserialize)]
pub struct MailSettings {
    #[serde(default = "default_mail_host")]
    pub host: String,
    #[serde(default = "default_mail_port")]
    pub port: u16,
    #[serde(default)]
    pub from: String,
    #[serde(default)]
    pub username: String,
    #[serde(default)]
    pub password: String,
}

impl Default for MailSettings {
    fn default() -> Self {
        Self {
            host: default_mail_host(),
            port: default_mail_port(),
            from: String::new(),
            username: String::new(),
            password: String::new(),
        }
    }
}

fn default_mail_host() -> String {
    "localhost".to_string()
}

fn default_mail_port() -> u16 {
    25
}

#[derive(Debug, Clone, Deserialize, PartialEq)]
#[serde(rename_all = "lowercase")]
pub enum QueueDriver {
    Redis,
    File,
}

impl Default for QueueDriver {
    fn default() -> Self {
        QueueDriver::File
    }
}

#[derive(Debug, Clone, Deserialize)]
pub struct QueueSettings {
    #[serde(default)]
    pub use_compression: bool,
    #[serde(default = "default_consumers")]
    pub primary_queue_consumers: usize,
    #[serde(default = "default_consumers")]
    pub secondary_queue_consumers: usize,
    #[serde(default)]
    pub queue_driver: QueueDriver,
    #[serde(default = "default_queue_name")]
    pub queue_name: String,
    #[serde(default)]
    pub redis: RedisSettings,
    #[serde(default)]
    pub file: FileSettings,
}

impl Default for QueueSettings {
    fn default() -> Self {
        Self {
            use_compression: false,
            primary_queue_consumers: default_consumers(),
            secondary_queue_consumers: default_consumers(),
            queue_driver: QueueDriver::default(),
            queue_name: default_queue_name(),
            redis: RedisSettings::default(),
            file: FileSettings::default(),
        }
    }
}

fn default_consumers() -> usize {
    8
}

fn default_queue_name() -> String {
    "InEngineQueue".to_string()
}

#[derive(Debug, Clone, Deserialize)]
pub struct RedisSettings {
    #[serde(default = "default_redis_host")]
    pub host: String,
    #[serde(default = "default_redis_port")]
    pub port: u16,
    #[serde(default)]
    pub database: u8,
    #[serde(default)]
    pub password: String,
}

impl Default for RedisSettings {
    fn default() -> Self {
        Self {
            host: default_redis_host(),
            port: default_redis_port(),
            database: 0,
            password: String::new(),
        }
    }
}

fn default_redis_host() -> String {
    "127.0.0.1".to_string()
}

fn default_redis_port() -> u16 {
    6379
}

#[derive(Debug, Clone, Deserialize)]
pub struct FileSettings {
    #[serde(default = "default_file_base_path")]
    pub base_path: String,
}

impl Default for FileSettings {
    fn default() -> Self {
        Self {
            base_path: default_file_base_path(),
        }
    }
}

fn default_file_base_path() -> String {
    "../".to_string()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn parse_valid_toml() {
        // Arrange
        let toml_str = r#"
[mail]
host = "localhost"
port = 1025
from = "no-reply@inengine.net"

[queue]
use_compression = false
primary_queue_consumers = 1
secondary_queue_consumers = 1
queue_driver = "file"
queue_name = "InEngineQueue"

[queue.redis]
host = "127.0.0.1"
port = 6379
database = 0
password = ""

[queue.file]
base_path = "../"
"#;

        // Act
        let settings: Settings = toml::from_str(toml_str).unwrap();

        // Assert
        assert_eq!(settings.mail.host, "localhost");
        assert_eq!(settings.mail.port, 1025);
        assert_eq!(settings.mail.from, "no-reply@inengine.net");
        assert!(!settings.queue.use_compression);
        assert_eq!(settings.queue.primary_queue_consumers, 1);
        assert_eq!(settings.queue.secondary_queue_consumers, 1);
        assert_eq!(settings.queue.queue_driver, QueueDriver::File);
        assert_eq!(settings.queue.queue_name, "InEngineQueue");
        assert_eq!(settings.queue.redis.host, "127.0.0.1");
        assert_eq!(settings.queue.redis.port, 6379);
        assert_eq!(settings.queue.redis.database, 0);
        assert_eq!(settings.queue.file.base_path, "../");
    }

    #[test]
    fn parse_minimal_toml_uses_defaults() {
        // Arrange
        let toml_str = "";

        // Act
        let settings: Settings = toml::from_str(toml_str).unwrap();

        // Assert
        assert_eq!(settings.mail.host, "localhost");
        assert_eq!(settings.mail.port, 25);
        assert_eq!(settings.queue.primary_queue_consumers, 8);
        assert_eq!(settings.queue.secondary_queue_consumers, 8);
        assert_eq!(settings.queue.queue_driver, QueueDriver::File);
        assert_eq!(settings.queue.queue_name, "InEngineQueue");
        assert_eq!(settings.queue.redis.host, "127.0.0.1");
        assert_eq!(settings.queue.redis.port, 6379);
        assert_eq!(settings.queue.file.base_path, "../");
    }

    #[test]
    fn defaults_match_expected_values() {
        // Arrange
        let defaults = QueueSettings::default();

        // Act / Assert
        assert_eq!(defaults.primary_queue_consumers, 8);
        assert_eq!(defaults.secondary_queue_consumers, 8);
        assert_eq!(defaults.queue_driver, QueueDriver::File);
        assert_eq!(defaults.queue_name, "InEngineQueue");
        assert_eq!(defaults.redis.host, "127.0.0.1");
        assert_eq!(defaults.redis.port, 6379);
        assert_eq!(defaults.redis.database, 0);

        let mail_defaults = MailSettings::default();
        assert_eq!(mail_defaults.host, "localhost");
        assert_eq!(mail_defaults.port, 25);

        let file_defaults = FileSettings::default();
        assert_eq!(file_defaults.base_path, "../");
    }

    #[test]
    fn load_missing_file_returns_file_not_found() {
        // Arrange
        let path = "/nonexistent/path/inengine.toml";

        // Act
        let result = Settings::load(path);

        // Assert
        assert!(result.is_err());
        let err = result.unwrap_err();
        assert!(
            matches!(err, ConfigError::FileNotFound { .. }),
            "expected FileNotFound, got: {err}"
        );
    }

    #[test]
    fn load_invalid_toml_returns_parse_error() {
        // Arrange
        let dir = tempfile::tempdir().unwrap();
        let path = dir.path().join("bad.toml");
        std::fs::write(&path, "[invalid toml !!!").unwrap();

        // Act
        let result = Settings::load(path.to_str().unwrap());

        // Assert
        assert!(result.is_err());
        let err = result.unwrap_err();
        assert!(
            matches!(err, ConfigError::Parse(_)),
            "expected Parse, got: {err}"
        );
    }

    #[test]
    fn reject_invalid_queue_driver() {
        // Arrange
        let toml_str = r#"
[queue]
queue_driver = "postgres"
"#;

        // Act
        let result: Result<Settings, _> = toml::from_str(toml_str);

        // Assert
        assert!(result.is_err());
    }

    #[test]
    fn snake_case_field_names_deserialize() {
        // Arrange
        let toml_str = r#"
[exec_whitelist]
hello = "/usr/bin/hello"

[queue]
use_compression = true
primary_queue_consumers = 4
secondary_queue_consumers = 2
queue_driver = "redis"
queue_name = "MyQueue"
"#;

        // Act
        let settings: Settings = toml::from_str(toml_str).unwrap();

        // Assert
        assert_eq!(
            settings.exec_whitelist.get("hello").unwrap(),
            "/usr/bin/hello"
        );
        assert!(settings.queue.use_compression);
        assert_eq!(settings.queue.primary_queue_consumers, 4);
        assert_eq!(settings.queue.secondary_queue_consumers, 2);
        assert_eq!(settings.queue.queue_driver, QueueDriver::Redis);
        assert_eq!(settings.queue.queue_name, "MyQueue");
    }

    #[test]
    fn load_valid_file() {
        // Arrange
        let dir = tempfile::tempdir().unwrap();
        let path = dir.path().join("inengine.toml");
        std::fs::write(
            &path,
            "[queue]\nqueue_driver = \"redis\"\n",
        )
        .unwrap();

        // Act
        let settings = Settings::load(path.to_str().unwrap()).unwrap();

        // Assert
        assert_eq!(settings.queue.queue_driver, QueueDriver::Redis);
    }

    #[test]
    fn load_io_error_for_directory_path() {
        // Arrange
        let dir = tempfile::tempdir().unwrap();
        let path = dir.path().to_str().unwrap();

        // Act
        let result = Settings::load(path);

        // Assert
        assert!(result.is_err());
        let err = result.unwrap_err();
        assert!(
            matches!(err, ConfigError::Io(_)),
            "expected Io error, got: {err}"
        );
    }
}
