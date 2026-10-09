use confval::keyword_enum;

keyword_enum!(
    /// The backend that stores queued messages, set by `queue.queue_driver`.
    pub QueueDriver, {
    /// A Redis server, keyword "redis". Needs Redis 6.2 or later.
    Redis => "redis",
    /// Files under `queue.file.base_path`, keyword "file".
    File  => "file",
});

keyword_enum!(
    /// How mail reaches the SMTP server. `Plain` is the keyword "none", a name that does not
    /// read like `Option::None`.
    pub MailEncryption, {
    /// No encryption, keyword "none". Mail travels in plain text.
    Plain    => "none",
    /// Keyword "starttls". The connection starts in plain text and upgrades to TLS. A server
    /// that cannot upgrade is refused.
    StartTls => "starttls",
    /// Keyword "tls". The connection uses TLS from the first byte.
    Tls      => "tls",
});
