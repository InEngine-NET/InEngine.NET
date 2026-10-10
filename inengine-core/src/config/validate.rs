use std::net::IpAddr;
use std::path::Path;

use confval::prelude::*;

use super::spec::{FileSpec, MailSpec, PluginLimitsSpec, QueueSpec, RedisSpec, SettingsSpec};
use super::vocabulary::MailEncryption;
use crate::BUILTIN_PLUGIN_NAME;

const REDIS_DEFAULT_DATABASES: i64 = 16;

impl Validate for SettingsSpec {
    fn validate(&self, report: &mut Report) {
        for (name, dir) in &self.plugins {
            if name == BUILTIN_PLUGIN_NAME {
                report
                    .error(format!(
                        "the plugin name '{BUILTIN_PLUGIN_NAME}' is reserved for built-in commands"
                    ))
                    .at(dir.span)
                    .help("Choose another name for this plugin.")
                    .emit();
            }
            check_path_entry(
                report,
                &format!("plugin \"{name}\""),
                dir,
                "The server skips this plugin.",
            );
        }
        for (name, path) in &self.exec_whitelist {
            check_path_entry(
                report,
                &format!("exec whitelist entry \"{name}\""),
                path,
                "The exec command fails for this entry.",
            );
        }
    }
}

impl Validate for MailSpec {
    fn validate(&self, report: &mut Report) {
        let from = &self.from;
        if !from.value.is_empty() && from.value.parse::<lettre::message::Mailbox>().is_err() {
            report
                .error(format!("\"{}\" is not a valid sender address", from.value))
                .at(from.span)
                .help("Use an address such as no-reply@example.com, or leave it empty.")
                .emit();
        }

        let has_username = !self.username.value.is_empty();
        let has_password = !self.password.value.is_empty();
        if has_username && !has_password {
            report
                .warning("a mail user name is set without a password")
                .at(self.username.span)
                .help("Set the password, or remove the user name to send without authentication.")
                .emit();
        }
        if has_password && !has_username {
            report
                .warning("the mail password is ignored because no user name is set")
                .at(self.password.span)
                .help("Set the user name, or remove the password.")
                .emit();
        }
        self.check_encryption(report, has_username);
    }
}

impl MailSpec {
    fn check_encryption(&self, report: &mut Report, has_username: bool) {
        let port = &self.port;
        let Ok(encryption) = MailEncryption::try_from(self.encryption.value.as_str()) else {
            return;
        };
        match encryption {
            MailEncryption::Plain if has_username && !is_loopback_host(&self.host.value) => {
                report
                    .error("the mail password would cross the network in plain text")
                    .at(self.username.span)
                    .related(self.host.span, "the mail server")
                    .help("Set encryption to \"starttls\" or \"tls\".")
                    .emit();
            }
            MailEncryption::Tls if port.value == 25 || port.value == 587 => {
                report
                    .warning(format!(
                        "mail port {} usually expects \"starttls\" or \"none\", not \"tls\"",
                        port.value
                    ))
                    .at(port.span)
                    .help("Use port 465 for \"tls\".")
                    .emit();
            }
            MailEncryption::StartTls if port.value == 465 => {
                report
                    .warning("mail port 465 usually expects \"tls\", not \"starttls\"")
                    .at(port.span)
                    .help("Use port 587 for \"starttls\".")
                    .emit();
            }
            _ => {}
        }
    }
}

fn is_loopback_host(host: &str) -> bool {
    host.eq_ignore_ascii_case("localhost")
        || host
            .parse::<IpAddr>()
            .is_ok_and(|address| address.is_loopback())
}

impl Validate for QueueSpec {
    fn validate(&self, report: &mut Report) {
        let primary = &self.primary_queue_consumers;
        let secondary = &self.secondary_queue_consumers;
        if primary.value == 0 && secondary.value == 0 {
            report
                .warning("the server starts no queue consumers")
                .at(primary.span)
                .related(secondary.span, "secondary consumers are also zero")
                .help("Set at least one consumer count above zero to process queued commands.")
                .emit();
        }
    }
}

impl Validate for RedisSpec {
    fn validate(&self, report: &mut Report) {
        let database = &self.database;
        if database.value >= REDIS_DEFAULT_DATABASES {
            report
                .warning(format!(
                    "database {} is outside the {REDIS_DEFAULT_DATABASES} databases a default Redis server provides",
                    database.value
                ))
                .at(database.span)
                .help("Use 0 to 15, or raise `databases` in the Redis server configuration.")
                .emit();
        }
    }
}

impl Validate for FileSpec {
    fn validate(&self, _report: &mut Report) {}
}

impl Validate for PluginLimitsSpec {
    fn validate(&self, _report: &mut Report) {}
}

fn check_path_entry(
    report: &mut Report,
    label: &str,
    path: &Located<String>,
    effect_if_missing: &str,
) {
    if path.value.trim().is_empty() {
        report
            .error(format!("{label} has an empty path"))
            .at(path.span)
            .help("Set the path, or remove the entry.")
            .emit();
    } else if !Path::new(&path.value).exists() {
        report
            .warning(format!(
                "{label} points to \"{}\", which does not exist",
                path.value
            ))
            .at(path.span)
            .help(format!(
                "A relative path resolves against the working directory. {effect_if_missing}"
            ))
            .emit();
    }
}
