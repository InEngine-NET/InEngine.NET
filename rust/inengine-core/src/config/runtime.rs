use std::collections::HashMap;
use std::time::Duration;

use confval::prelude::*;

use super::spec::{FileSpec, MailSpec, PluginLimitsSpec, QueueSpec, RedisSpec, SettingsSpec};
use super::vocabulary::QueueDriver;

#[derive(Debug, Clone, confval::Config)]
#[confval(lower_from = SettingsSpec)]
pub struct Settings {
    pub plugins: HashMap<String, String>,
    pub exec_whitelist: HashMap<String, String>,
    #[confval(nested)]
    pub mail: MailSettings,
    #[confval(nested)]
    pub queue: QueueSettings,
    #[confval(nested)]
    pub plugin_limits: PluginLimitsSettings,
}

#[derive(Debug, Clone, confval::Config)]
#[confval(lower_from = MailSpec)]
pub struct MailSettings {
    pub host: String,
    #[confval(lower(from = port, with = narrow::i64_to_u16))]
    pub port: u16,
    pub from: String,
    pub username: String,
    pub password: String,
}

#[derive(Debug, Clone, confval::Config)]
#[confval(lower_from = QueueSpec)]
pub struct QueueSettings {
    pub use_compression: bool,
    #[confval(lower(from = primary_queue_consumers, with = narrow::i64_to_usize))]
    pub primary_queue_consumers: usize,
    #[confval(lower(from = secondary_queue_consumers, with = narrow::i64_to_usize))]
    pub secondary_queue_consumers: usize,
    #[confval(lower(from = queue_driver, with = narrow::keyword::<QueueDriver>))]
    pub queue_driver: QueueDriver,
    pub queue_name: String,
    #[confval(lower(from = max_retries, with = narrow::i64_to_u32))]
    pub max_retries: u32,
    pub recover_on_start: bool,
    #[confval(nested)]
    pub redis: RedisSettings,
    #[confval(nested)]
    pub file: FileSettings,
}

#[derive(Debug, Clone, confval::Config)]
#[confval(lower_from = RedisSpec)]
pub struct RedisSettings {
    pub host: String,
    #[confval(lower(from = port, with = narrow::i64_to_u16))]
    pub port: u16,
    #[confval(lower(from = database, with = i64_to_u8))]
    pub database: u8,
    pub password: String,
}

#[derive(Debug, Clone, confval::Config)]
#[confval(lower_from = FileSpec)]
pub struct FileSettings {
    pub base_path: String,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, confval::Config)]
#[confval(lower_from = PluginLimitsSpec)]
pub struct PluginLimitsSettings {
    #[confval(lower(from = timeout_seconds, with = narrow::i64_secs_to_duration))]
    pub timeout: Duration,
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
