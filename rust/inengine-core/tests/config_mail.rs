mod common;

use common::config_issues as issues;
use confval::diagnostic::Severity;
use confval::prelude::*;
use inengine_core::config::{self, Loaded, MailEncryption};

#[test]
fn credentials_without_a_partner_warn_at_the_set_field() {
    // Arrange
    let only_user = "[mail]\nusername = \"mailer\"\n";
    let only_password = "[mail]\npassword = \"secret\"\n";

    // Act
    let user_loaded = config::from_text("user.toml", only_user);
    let password_loaded = config::from_text("password.toml", only_password);

    // Assert
    let user_issues = issues(&user_loaded);
    assert_eq!(user_issues.len(), 1);
    assert_eq!(user_issues[0].0, Severity::Warning);
    assert_eq!(user_issues[0].1, Some(2));
    assert!(user_issues[0].2.contains("without a password"));
    let password_issues = issues(&password_loaded);
    assert_eq!(password_issues.len(), 1);
    assert_eq!(password_issues[0].0, Severity::Warning);
    assert_eq!(password_issues[0].1, Some(2));
    assert!(password_issues[0].2.contains("ignored"));
    assert!(user_loaded.settings.is_some());
    assert!(password_loaded.settings.is_some());
}

#[test]
fn encryption_defaults_to_none() {
    // Arrange
    let text = "[mail]\nhost = \"localhost\"\n";

    // Act
    let loaded = config::from_text("inengine.toml", text);

    // Assert
    assert!(!loaded.report.has_issues(), "{:#?}", issues(&loaded));
    let settings = loaded.settings.expect("settings lower");
    assert_eq!(settings.mail.encryption, MailEncryption::Plain);
}

#[test]
fn plain_credentials_to_a_remote_host_are_an_error() {
    // Arrange
    let text =
        "[mail]\nhost = \"smtp.example.com\"\nusername = \"mailer\"\npassword = \"secret\"\n";

    // Act
    let loaded = config::from_text("inengine.toml", text);

    // Assert
    assert!(loaded.settings.is_none());
    let issue = match loaded.report.issues() {
        [issue] => issue,
        other => panic!("expected one issue, found {other:#?}"),
    };
    assert_eq!(issue.severity, Severity::Error);
    assert_eq!(
        issue.message,
        "the mail password would cross the network in plain text"
    );
    let line_of = |span: Span| {
        loaded
            .sources
            .get(span.source)
            .map(|source| source.line_column(span.start).0)
    };
    assert_eq!(issue.span.and_then(line_of), Some(3));
    assert_eq!(issue.related.len(), 1);
    assert_eq!(line_of(issue.related[0].0), Some(2));
    assert_eq!(issue.related[0].1, "the mail server");
    assert_eq!(
        issue.help.as_deref(),
        Some("Set encryption to \"starttls\" or \"tls\".")
    );
}

#[test]
fn plain_credentials_to_localhost_are_allowed() {
    // Arrange
    let names = ["localhost", "127.0.0.1", "::1"];

    // Act
    let loaded: Vec<Loaded> = names
        .iter()
        .map(|host| {
            let text = format!(
                "[mail]\nhost = \"{host}\"\nusername = \"mailer\"\npassword = \"secret\"\n"
            );
            config::from_text("inengine.toml", &text)
        })
        .collect();

    // Assert
    for (host, loaded) in names.iter().zip(&loaded) {
        assert!(!loaded.report.has_issues(), "{host}: {:#?}", issues(loaded));
        assert!(loaded.settings.is_some(), "{host}");
    }
}

#[test]
fn a_port_that_does_not_match_the_encryption_warns() {
    // Arrange
    let tls_on_25 = "[mail]\nport = 25\nencryption = \"tls\"\n";
    let tls_on_587 = "[mail]\nport = 587\nencryption = \"tls\"\n";
    let starttls_on_465 = "[mail]\nport = 465\nencryption = \"starttls\"\n";

    // Act
    let tls_25_loaded = config::from_text("tls25.toml", tls_on_25);
    let tls_loaded = config::from_text("tls.toml", tls_on_587);
    let starttls_loaded = config::from_text("starttls.toml", starttls_on_465);

    // Assert
    assert_eq!(
        issues(&tls_25_loaded),
        [(
            Severity::Warning,
            Some(2),
            "mail port 25 usually expects \"starttls\" or \"none\", not \"tls\"".to_string()
        )]
    );
    assert_eq!(
        issues(&tls_loaded),
        [(
            Severity::Warning,
            Some(2),
            "mail port 587 usually expects \"starttls\" or \"none\", not \"tls\"".to_string()
        )]
    );
    assert_eq!(
        issues(&starttls_loaded),
        [(
            Severity::Warning,
            Some(2),
            "mail port 465 usually expects \"tls\", not \"starttls\"".to_string()
        )]
    );
    assert!(tls_loaded.settings.is_some());
    assert!(starttls_loaded.settings.is_some());
}
