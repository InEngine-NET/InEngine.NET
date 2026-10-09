use confval::keyword_enum;

keyword_enum!(
    pub QueueDriver, {
    Redis => "redis",
    File  => "file",
});

keyword_enum!(
    /// How mail reaches the SMTP server. `Plain` is the keyword "none", a name that does not
    /// read like `Option::None`.
    pub MailEncryption, {
    Plain    => "none",
    StartTls => "starttls",
    Tls      => "tls",
});
