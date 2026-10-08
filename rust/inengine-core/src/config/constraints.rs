use confval::{length_constraint, range_constraint, Format};

range_constraint!(
    /// A TCP port.
    pub(crate) PORT, i64, min: 1, max: 65535
);
range_constraint!(
    /// The number of consumer tasks for one queue. Zero turns the queue's consumers off.
    pub(crate) CONSUMERS, i64, min: 0, max: 1024
);
range_constraint!(
    /// The number of attempts after the first one for a failed message.
    pub(crate) MAX_RETRIES, i64, min: 0, max: 100
);
range_constraint!(
    /// A Redis logical database index.
    pub(crate) REDIS_DATABASE, i64, min: 0, max: 255
);
length_constraint!(
    /// A host name is required, and a DNS name is at most 253 characters.
    pub(crate) HOSTNAME_LEN, min: 1, max: 253
);
length_constraint!(
    /// A file system path is required. The upper bound matches the Linux path limit.
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
