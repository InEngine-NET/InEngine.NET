use confval::{length_constraint, range_constraint, Format};

range_constraint!(pub(crate) PORT, i64, min: 1, max: 65535);
range_constraint!(pub(crate) CONSUMERS, i64, min: 0, max: 1024);
range_constraint!(pub(crate) MAX_RETRIES, i64, min: 0, max: 100);
range_constraint!(pub(crate) REDIS_DATABASE, i64, min: 0, max: 255);
range_constraint!(pub(crate) PLUGIN_TIMEOUT_SECONDS, i64, min: 1, max: 86400);
range_constraint!(
    /// A Rust component starts with about 1.1 MiB of memory and grows while it runs. The
    /// minimum leaves room for that growth.
    pub(crate) PLUGIN_MEMORY_MIB, i64, min: 16, max: 4096
);
length_constraint!(
    pub(crate) HOSTNAME_LEN, min: 1, max: 253
);
length_constraint!(
    /// The upper bound matches the Linux path limit.
    pub(crate) PATH_LEN, min: 1, max: 4096
);

/// A queue name that is safe to use as part of a file name and a Redis key.
pub struct QueueName;

impl Format for QueueName {
    const NAME: &'static str = "queue name of letters, digits, '_', or '-'";

    fn check(value: &str) -> bool {
        !value.is_empty()
            && value
                .chars()
                .all(|c| c.is_ascii_alphanumeric() || c == '_' || c == '-')
    }
}
