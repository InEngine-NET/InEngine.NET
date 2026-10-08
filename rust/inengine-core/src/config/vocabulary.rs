use confval::keyword_enum;

keyword_enum!(
    pub QueueDriver, {
    Redis => "redis",
    File  => "file",
});
