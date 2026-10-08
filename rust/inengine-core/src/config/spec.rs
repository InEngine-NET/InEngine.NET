use std::collections::BTreeMap;

use confval::prelude::*;

use super::constraints::{
    QueueName, CONSUMERS, HOSTNAME_LEN, MAX_RETRIES, PATH_LEN, PORT, REDIS_DATABASE,
};
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
    #[confval(nested, default)]
    pub queue: Located<QueueSpec>,
}

#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct MailSpec {
    #[confval(default = "localhost".to_string(), length = HOSTNAME_LEN)]
    pub host: Located<String>,
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
}

#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct QueueSpec {
    #[confval(default = false)]
    pub use_compression: Located<bool>,
    /// Zero turns the queue off.
    #[confval(default = 8, range = CONSUMERS)]
    pub primary_queue_consumers: Located<i64>,
    /// Zero turns the queue off.
    #[confval(default = 8, range = CONSUMERS)]
    pub secondary_queue_consumers: Located<i64>,
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
    #[confval(nested, default)]
    pub file: Located<FileSpec>,
}

#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct RedisSpec {
    #[confval(default = "127.0.0.1".to_string(), length = HOSTNAME_LEN)]
    pub host: Located<String>,
    #[confval(default = 6379, range = PORT)]
    pub port: Located<i64>,
    #[confval(default = 0, range = REDIS_DATABASE)]
    pub database: Located<i64>,
    /// Leave empty when the server has no password.
    #[confval(default = String::new())]
    pub password: Located<String>,
}

#[derive(confval::Spec)]
#[confval(derive_default)]
pub struct FileSpec {
    /// A relative path resolves against the working directory.
    #[confval(default = "../".to_string(), length = PATH_LEN)]
    pub base_path: Located<String>,
}
