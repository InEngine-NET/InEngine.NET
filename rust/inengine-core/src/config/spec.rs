use std::collections::BTreeMap;

use confval::prelude::*;

use super::constraints::{QueueName, CONSUMERS, HOSTNAME_LEN, PATH_LEN, PORT, REDIS_DATABASE};
use super::vocabulary::QueueDriver;

#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct SettingsSpec {
    /// Plugin directories keyed by plugin name. The server loads every .wasm file in each
    /// directory. A relative path resolves against the working directory.
    #[confval(map, default)]
    pub plugins: BTreeMap<String, Located<String>>,
    /// Executables the exec command may run, keyed by the name a caller passes.
    #[confval(map, default)]
    pub exec_whitelist: BTreeMap<String, Located<String>>,
    /// The SMTP server that sends command notification mail.
    #[confval(nested, default)]
    pub mail: Located<MailSpec>,
    /// The queue that holds published commands.
    #[confval(nested, default)]
    pub queue: Located<QueueSpec>,
}

#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct MailSpec {
    /// The SMTP host name.
    #[confval(default = "localhost".to_string(), length = HOSTNAME_LEN)]
    pub host: Located<String>,
    /// The SMTP port.
    #[confval(default = 25, range = PORT)]
    pub port: Located<i64>,
    /// The sender address. Leave empty when no command sends mail.
    #[confval(default = String::new())]
    pub from: Located<String>,
    /// The SMTP user name. Leave empty to send without authentication.
    #[confval(default = String::new())]
    pub username: Located<String>,
    /// The SMTP password. Used only when a user name is set.
    #[confval(default = String::new())]
    pub password: Located<String>,
}

#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct QueueSpec {
    /// Compress message payloads before they are queued.
    #[confval(default = false)]
    pub use_compression: Located<bool>,
    /// The number of consumers for the primary queue.
    #[confval(default = 8, range = CONSUMERS)]
    pub primary_queue_consumers: Located<i64>,
    /// The number of consumers for the secondary queue.
    #[confval(default = 8, range = CONSUMERS)]
    pub secondary_queue_consumers: Located<i64>,
    /// The queue backend, "file" or "redis".
    #[confval(default = "file".to_string(), keywords = QueueDriver)]
    pub queue_driver: Located<String>,
    /// The base name of the queue. It becomes part of file names and Redis keys.
    #[confval(default = "InEngineQueue".to_string(), format = QueueName)]
    pub queue_name: Located<String>,
    /// The Redis server. Used only when queue_driver is "redis".
    #[confval(nested, default)]
    pub redis: Located<RedisSpec>,
    /// The file queue. Used only when queue_driver is "file".
    #[confval(nested, default)]
    pub file: Located<FileSpec>,
}

#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct RedisSpec {
    /// The Redis host name or address.
    #[confval(default = "127.0.0.1".to_string(), length = HOSTNAME_LEN)]
    pub host: Located<String>,
    /// The Redis port.
    #[confval(default = 6379, range = PORT)]
    pub port: Located<i64>,
    /// The Redis logical database index.
    #[confval(default = 0, range = REDIS_DATABASE)]
    pub database: Located<i64>,
    /// The Redis password. Leave empty when the server has no password.
    #[confval(default = String::new())]
    pub password: Located<String>,
}

#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct FileSpec {
    /// The directory that holds the queue files. A relative path resolves against the
    /// working directory.
    #[confval(default = "../".to_string(), length = PATH_LEN)]
    pub base_path: Located<String>,
}
