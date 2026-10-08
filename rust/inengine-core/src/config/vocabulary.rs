use confval::keyword_enum;

keyword_enum!(
    /// The backend that stores queued messages.
    pub QueueDriver, {
    Redis => "redis",
    File  => "file",
});
