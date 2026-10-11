use std::collections::{BTreeMap, HashMap};
use std::path::PathBuf;
use std::time::Duration;

use confval::prelude::*;

use super::spec::{FileSpec, MailSpec, PluginLimitsSpec, QueueSpec, RedisSpec, SettingsSpec};
use super::vocabulary::{MailEncryption, QueueDriver};

/// The validated configuration that the server and the commands read at run time.
///
/// Get it from [`Settings::load`], or from the [`Loaded`](super::Loaded) value that
/// [`load`](super::load) returns when you also want the warnings.
#[derive(Debug, Clone, confval::Config)]
#[confval(lower_from = SettingsSpec)]
pub struct Settings {
    /// Plugin directories keyed by plugin name. The server loads every `.wasm` file in each
    /// directory.
    #[confval(lower(from = plugins, with = to_path_map))]
    pub plugins: HashMap<String, PathBuf>,
    /// Executables the exec command may run, keyed by the name a caller passes.
    #[confval(lower(from = exec_whitelist, with = to_path_map))]
    pub exec_whitelist: HashMap<String, PathBuf>,
    /// The SMTP server that sends command output by mail.
    #[confval(nested)]
    pub mail: MailSettings,
    /// The queue backend, its consumers, and its retry rules.
    #[confval(nested)]
    pub queue: QueueSettings,
    /// The time and memory limits that apply to every plugin call.
    #[confval(nested)]
    pub plugin_limits: PluginLimitsSettings,
}

/// The SMTP server that a scheduled job uses when it mails its output.
#[derive(Debug, Clone, confval::Config)]
#[confval(lower_from = MailSpec)]
pub struct MailSettings {
    /// The SMTP server hostname.
    pub host: String,
    /// The SMTP server port.
    #[confval(lower(from = port, with = narrow::i64_to_u16))]
    pub port: u16,
    /// The sender address. Empty when no command sends mail.
    pub from: String,
    /// The SMTP user name. Empty means mail is sent without authentication.
    pub username: String,
    /// The SMTP password. It is used only when `username` is set.
    pub password: String,
    /// How the connection to the SMTP server is secured.
    #[confval(lower(from = encryption, with = narrow::keyword::<MailEncryption>))]
    pub encryption: MailEncryption,
}

/// The queue backend, the consumer counts, and the retry rules for queued messages.
#[derive(Debug, Clone, confval::Config)]
#[confval(lower_from = QueueSpec)]
pub struct QueueSettings {
    /// When true, the command in each published message is compressed with gzip.
    pub use_compression: bool,
    /// The number of consumer tasks for the primary queue. Zero turns the queue off.
    #[confval(lower(from = primary_queue_consumers, with = narrow::i64_to_usize))]
    pub primary_queue_consumers: usize,
    /// The number of consumer tasks for the secondary queue. Zero turns the queue off.
    #[confval(lower(from = secondary_queue_consumers, with = narrow::i64_to_usize))]
    pub secondary_queue_consumers: usize,
    /// The backend that stores queued messages.
    #[confval(lower(from = queue_driver, with = narrow::keyword::<QueueDriver>))]
    pub queue_driver: QueueDriver,
    /// The base name of the queue. It becomes part of file names and Redis keys.
    pub queue_name: String,
    /// How many times a failed message is retried before it moves to the failed stage. Zero
    /// means a failed message is not retried.
    #[confval(lower(from = max_retries, with = narrow::i64_to_u32))]
    pub max_retries: u32,
    /// When true, the server moves in-progress messages back to pending when it starts.
    pub recover_on_start: bool,
    /// The Redis connection. It is used only when `queue_driver` is [`QueueDriver::Redis`].
    #[confval(nested)]
    pub redis: RedisSettings,
    /// The file queue location. It is used only when `queue_driver` is [`QueueDriver::File`].
    #[confval(nested)]
    pub file: FileSettings,
}

/// The connection to the Redis server that holds the queue.
#[derive(Debug, Clone, confval::Config)]
#[confval(lower_from = RedisSpec)]
pub struct RedisSettings {
    /// The Redis hostname.
    pub host: String,
    /// The Redis port.
    #[confval(lower(from = port, with = narrow::i64_to_u16))]
    pub port: u16,
    /// The Redis database number, from 0 to 255.
    #[confval(lower(from = database, with = i64_to_u8))]
    pub database: u8,
    /// The Redis password. Empty means the server has no password.
    pub password: String,
}

/// Where the file queue stores its messages.
#[derive(Debug, Clone, confval::Config)]
#[confval(lower_from = FileSpec)]
pub struct FileSettings {
    /// The directory that holds the queue directories. A relative path resolves against the
    /// working directory.
    #[confval(lower(from = base_path, with = to_path))]
    pub base_path: PathBuf,
}

/// The time and memory limits that apply to every plugin call. The default is 900 seconds and
/// 256 MiB.
#[derive(Debug, Clone, Copy, PartialEq, Eq, confval::Config)]
#[confval(lower_from = PluginLimitsSpec)]
pub struct PluginLimitsSettings {
    /// The longest time one plugin call may run. A call that runs longer is stopped and fails.
    #[confval(lower(from = timeout_seconds, with = narrow::i64_secs_to_duration))]
    pub timeout: Duration,
    /// The memory budget of one plugin instance, in bytes. A call that needs more fails.
    #[confval(lower(from = memory_mib, with = mib_to_bytes))]
    pub memory_bytes: usize,
}

impl Default for PluginLimitsSettings {
    fn default() -> Self {
        Self {
            timeout: Duration::from_secs(900),
            memory_bytes: 256 * BYTES_PER_MIB,
        }
    }
}

fn to_path(value: &Located<String>, _report: &mut Report) -> Option<PathBuf> {
    Some(PathBuf::from(&value.value))
}

fn to_path_map(
    values: &BTreeMap<String, Located<String>>,
    report: &mut Report,
) -> Option<HashMap<String, PathBuf>> {
    values
        .iter()
        .map(|(name, path)| Some((name.clone(), to_path(path, report)?)))
        .collect()
}

pub(crate) const BYTES_PER_MIB: usize = 1024 * 1024;

fn mib_to_bytes(value: &Located<i64>, report: &mut Report) -> Option<usize> {
    let mib = narrow::i64_to_usize(value, report)?;
    match mib.checked_mul(BYTES_PER_MIB) {
        Some(bytes) => Some(bytes),
        None => {
            report
                .error(format!(
                    "{mib} MiB does not fit in memory on this platform, so a validation rule is missing"
                ))
                .at(value.span)
                .emit();
            None
        }
    }
}

fn i64_to_u8(value: &Located<i64>, report: &mut Report) -> Option<u8> {
    match u8::try_from(value.value) {
        Ok(narrowed) => Some(narrowed),
        Err(_) => {
            report
                .error(format!(
                    "value {} is out of range for u8, so a validation rule is missing",
                    value.value
                ))
                .at(value.span)
                .emit();
            None
        }
    }
}
