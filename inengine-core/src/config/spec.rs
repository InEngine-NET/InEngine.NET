use std::collections::BTreeMap;

use confval::prelude::*;

use super::constraints::{
    CONSUMERS, HOSTNAME_LEN, MAX_RETRIES, PATH_LEN, PLUGIN_MEMORY_MIB, PLUGIN_TIMEOUT_SECONDS,
    PORT, QueueName, REDIS_DATABASE,
};
use super::vocabulary::{MailEncryption, QueueDriver};

/// The parsed `inengine.toml` before validation.
///
/// Pass it to [`from_spec`](super::from_spec) to validate it and get
/// [`Settings`](super::Settings).
#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct SettingsSpec {
    /// Plugin directories keyed by plugin name. The server loads every `.wasm` file in each
    /// directory. A relative path resolves against the working directory.
    #[confval(map, default)]
    pub plugins: BTreeMap<String, Located<String>>,
    /// Executables the exec command may run, keyed by the name a caller passes.
    #[confval(map, default)]
    pub exec_whitelist: BTreeMap<String, Located<String>>,
    /// The SMTP server that sends command notification mail.
    #[confval(nested, default)]
    pub mail: Located<MailSpec>,
    /// The queue backend, its consumers, and its retry rules.
    #[confval(nested, default)]
    pub queue: Located<QueueSpec>,
    /// Limits that apply to every plugin call.
    #[confval(nested, default)]
    pub plugin_limits: Located<PluginLimitsSpec>,
}

/// The `[mail]` section of `inengine.toml`.
#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct MailSpec {
    /// The SMTP server hostname. The default is "localhost".
    #[confval(default = "localhost".to_string(), length = HOSTNAME_LEN)]
    pub host: Located<String>,
    /// The SMTP server port, from 1 to 65535. The default is 25.
    #[confval(default = 25, range = PORT)]
    pub port: Located<i64>,
    /// Leave empty when no command sends mail.
    #[confval(default = String::new())]
    pub from: Located<String>,
    /// Leave empty to send without authentication.
    #[confval(default = String::new())]
    pub username: Located<String>,
    /// Used only when a user name is set.
    #[confval(default = String::new())]
    pub password: Located<String>,
    /// "none" sends in plain text, "starttls" upgrades the connection and refuses a server that
    /// cannot, and "tls" uses TLS from the first byte.
    #[confval(default = "none".to_string(), keywords = MailEncryption)]
    pub encryption: Located<String>,
}

/// The `[queue]` section of `inengine.toml`.
#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct QueueSpec {
    /// Compress the command in each published message with gzip. The default is false.
    #[confval(default = false)]
    pub use_compression: Located<bool>,
    /// Zero turns the queue off.
    #[confval(default = 8, range = CONSUMERS)]
    pub primary_queue_consumers: Located<i64>,
    /// Zero turns the queue off.
    #[confval(default = 8, range = CONSUMERS)]
    pub secondary_queue_consumers: Located<i64>,
    /// The queue backend, "file" or "redis". The default is "file".
    #[confval(default = "file".to_string(), keywords = QueueDriver)]
    pub queue_driver: Located<String>,
    /// Becomes part of file names and Redis keys.
    #[confval(default = "InEngineQueue".to_string(), format = QueueName)]
    pub queue_name: Located<String>,
    /// How many times a failed message is retried. A message that fails every attempt runs
    /// this many times plus one, then moves to the failed stage.
    #[confval(default = 0, range = MAX_RETRIES)]
    pub max_retries: Located<i64>,
    /// Move in-progress messages back to pending when the server starts. Enable this only
    /// when this server is the only consumer of the queue. Recovery cannot tell a message left
    /// by a crash from one that another consumer is still processing, so that message would
    /// run twice.
    #[confval(default = false)]
    pub recover_on_start: Located<bool>,
    /// Needs Redis 6.2 or later.
    #[confval(nested, default)]
    pub redis: Located<RedisSpec>,
    /// The location of the file queue. It is used only when `queue_driver` is "file".
    #[confval(nested, default)]
    pub file: Located<FileSpec>,
}

/// The `[queue.redis]` section of `inengine.toml`.
#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct RedisSpec {
    /// The Redis hostname. The default is "127.0.0.1".
    #[confval(default = "127.0.0.1".to_string(), length = HOSTNAME_LEN)]
    pub host: Located<String>,
    /// The Redis port, from 1 to 65535. The default is 6379.
    #[confval(default = 6379, range = PORT)]
    pub port: Located<i64>,
    /// The Redis database number, from 0 to 255. The default is 0.
    #[confval(default = 0, range = REDIS_DATABASE)]
    pub database: Located<i64>,
    /// Leave empty when the server has no password.
    #[confval(default = String::new())]
    pub password: Located<String>,
}

/// The `[queue.file]` section of `inengine.toml`.
#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct FileSpec {
    /// A relative path resolves against the working directory.
    #[confval(default = "../".to_string(), length = PATH_LEN)]
    pub base_path: Located<String>,
}

/// The `[plugin_limits]` section of `inengine.toml`.
#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct PluginLimitsSpec {
    /// The longest time one plugin call may run. A call that runs longer is stopped and fails.
    #[confval(default = 900, range = PLUGIN_TIMEOUT_SECONDS)]
    pub timeout_seconds: Located<i64>,
    /// The memory budget of one plugin instance, in MiB. A call that needs more fails.
    #[confval(default = 256, range = PLUGIN_MEMORY_MIB)]
    pub memory_mib: Located<i64>,
}
