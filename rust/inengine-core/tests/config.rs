use std::path::Path;

use confval::diagnostic::Severity;
use confval::prelude::*;
use inengine_core::config::{self, Loaded, QueueDriver, Settings, SettingsSpec};
use inengine_core::error::ConfigError;

fn issues(loaded: &Loaded) -> Vec<(Severity, Option<usize>, String)> {
    loaded
        .report
        .issues()
        .iter()
        .map(|issue| {
            let line = issue.span.and_then(|span| {
                loaded
                    .sources
                    .get(span.source)
                    .map(|source| source.line_column(span.start).0)
            });
            (issue.severity.clone(), line, issue.message.clone())
        })
        .collect()
}

fn defaults() -> Settings {
    config::from_spec(&SettingsSpec::default())
        .settings
        .expect("the default settings are valid")
}

fn assert_default_settings(settings: &Settings) {
    assert!(settings.plugins.is_empty());
    assert!(settings.exec_whitelist.is_empty());
    assert_eq!(settings.mail.host, "localhost");
    assert_eq!(settings.mail.port, 25);
    assert_eq!(settings.mail.from, "");
    assert_eq!(settings.mail.username, "");
    assert_eq!(settings.mail.password, "");
    assert!(!settings.queue.use_compression);
    assert_eq!(settings.queue.primary_queue_consumers, 8);
    assert_eq!(settings.queue.secondary_queue_consumers, 8);
    assert_eq!(settings.queue.queue_driver, QueueDriver::File);
    assert_eq!(settings.queue.queue_name, "InEngineQueue");
    assert_eq!(settings.queue.max_retries, 0);
    assert!(!settings.queue.recover_on_start);
    assert_eq!(settings.queue.redis.host, "127.0.0.1");
    assert_eq!(settings.queue.redis.port, 6379);
    assert_eq!(settings.queue.redis.database, 0);
    assert_eq!(settings.queue.redis.password, "");
    assert_eq!(settings.queue.file.base_path, "../");
}

#[test]
fn a_bad_file_reports_every_problem_at_once() {
    // Arrange
    let text = r#"[exec_whitelist]
hello = ""

[mail]
host = ""
port = 0
from = "not an address"

[queue]
primary_queue_consumers = -1
queue_driver = "postgres"
queue_name = "../escape"

[queue.redis]
database = 256
"#;

    // Act
    let loaded = config::from_text("inengine.toml", text);

    // Assert
    assert!(loaded.settings.is_none());
    let found = issues(&loaded);
    let errors: Vec<_> = found
        .iter()
        .filter(|(severity, _, _)| *severity == Severity::Error)
        .map(|(_, line, _)| *line)
        .collect();
    assert_eq!(
        errors,
        vec![
            Some(2),
            Some(5),
            Some(6),
            Some(7),
            Some(10),
            Some(11),
            Some(12),
            Some(15),
        ],
        "every bad value is reported at its own line: {found:#?}"
    );
}

#[test]
fn a_complete_file_lowers_to_the_values_it_names() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let plugin_dir = dir.path().join("sample");
    std::fs::create_dir(&plugin_dir).expect("plugin directory");
    let text = format!(
        r#"[plugins]
sample = "{plugin}"

[exec_whitelist]
shell = "/bin/sh"

[mail]
host = "smtp.example.com"
port = 587
from = "no-reply@example.com"
username = "mailer"
password = "secret"

[queue]
use_compression = true
primary_queue_consumers = 4
secondary_queue_consumers = 2
queue_driver = "redis"
queue_name = "MyQueue"
max_retries = 2
recover_on_start = true

[queue.redis]
host = "redis.internal"
port = 6380
database = 3
password = "hunter2"

[queue.file]
base_path = "/var/lib/inengine"
"#,
        plugin = plugin_dir.display()
    );

    // Act
    let loaded = config::from_text("inengine.toml", &text);

    // Assert
    assert!(!loaded.report.has_issues(), "{:#?}", issues(&loaded));
    let settings = loaded.settings.expect("settings lower");
    assert_eq!(
        settings.plugins.get("sample").map(String::as_str),
        Some(plugin_dir.to_str().expect("utf-8 path"))
    );
    assert_eq!(
        settings.exec_whitelist.get("shell").map(String::as_str),
        Some("/bin/sh")
    );
    assert_eq!(settings.mail.host, "smtp.example.com");
    assert_eq!(settings.mail.port, 587);
    assert_eq!(settings.mail.from, "no-reply@example.com");
    assert_eq!(settings.mail.username, "mailer");
    assert_eq!(settings.mail.password, "secret");
    assert!(settings.queue.use_compression);
    assert_eq!(settings.queue.primary_queue_consumers, 4);
    assert_eq!(settings.queue.secondary_queue_consumers, 2);
    assert_eq!(settings.queue.queue_driver, QueueDriver::Redis);
    assert_eq!(settings.queue.queue_name, "MyQueue");
    assert_eq!(settings.queue.max_retries, 2);
    assert!(settings.queue.recover_on_start);
    assert_eq!(settings.queue.redis.host, "redis.internal");
    assert_eq!(settings.queue.redis.port, 6380);
    assert_eq!(settings.queue.redis.database, 3);
    assert_eq!(settings.queue.redis.password, "hunter2");
    assert_eq!(settings.queue.file.base_path, "/var/lib/inengine");
}

#[test]
fn an_omitted_field_takes_its_default() {
    // Arrange
    let text = "[queue]\nqueue_driver = \"redis\"\n";

    // Act
    let loaded = config::from_text("inengine.toml", text);

    // Assert
    assert!(!loaded.report.has_issues(), "{:#?}", issues(&loaded));
    let settings = loaded.settings.expect("settings lower");
    assert_eq!(settings.queue.queue_driver, QueueDriver::Redis);
    assert_eq!(settings.queue.primary_queue_consumers, 8);
    assert_eq!(settings.queue.queue_name, "InEngineQueue");
    assert_eq!(settings.queue.redis.port, 6379);
    assert_eq!(settings.mail.host, "localhost");
}

#[test]
fn an_empty_file_yields_the_declared_defaults() {
    // Arrange
    let text = "";

    // Act
    let loaded = config::from_text("inengine.toml", text);

    // Assert
    assert!(!loaded.report.has_issues(), "{:#?}", issues(&loaded));
    assert_default_settings(&loaded.settings.expect("settings lower"));
}

#[test]
fn no_file_at_all_yields_the_declared_defaults() {
    // Arrange
    let spec = SettingsSpec::default();

    // Act
    let loaded = config::from_spec(&spec);

    // Assert
    assert!(!loaded.report.has_issues());
    assert_default_settings(&loaded.settings.expect("settings lower"));
}

#[test]
fn the_starter_file_names_every_default() {
    // Arrange
    let path = Path::new(env!("CARGO_MANIFEST_DIR")).join("../inengine.example.toml");

    // Act
    let loaded = config::load(&path).expect("the starter file is readable");

    // Assert
    assert!(!loaded.report.has_issues(), "{:#?}", issues(&loaded));
    assert_default_settings(&loaded.settings.expect("settings lower"));
}

#[test]
fn a_warning_does_not_stop_the_load() {
    // Arrange
    let text = "[queue.redis]\ndatabase = 20\n";

    // Act
    let loaded = config::from_text("inengine.toml", text);

    // Assert
    assert!(!loaded.report.has_errors());
    let found = issues(&loaded);
    assert_eq!(found.len(), 1);
    assert_eq!(found[0].0, Severity::Warning);
    assert_eq!(found[0].1, Some(2));
    assert!(found[0].2.contains("database 20"), "{}", found[0].2);
    let settings = loaded.settings.expect("settings lower despite the warning");
    assert_eq!(settings.queue.redis.database, 20);
}

#[test]
fn a_syntax_error_reports_rather_than_panics() {
    // Arrange
    let text = "[invalid toml !!!";

    // Act
    let loaded = config::from_text("inengine.toml", text);

    // Assert
    assert!(loaded.settings.is_none());
    assert!(loaded.report.has_errors());
    assert!(!loaded.render_plain().is_empty());
}

#[test]
fn an_unknown_queue_driver_is_rejected_at_its_line() {
    // Arrange
    let text = "[queue]\nqueue_driver = \"postgres\"\n";

    // Act
    let loaded = config::from_text("inengine.toml", text);

    // Assert
    assert!(loaded.settings.is_none());
    let found = issues(&loaded);
    assert_eq!(found.len(), 1);
    assert_eq!(found[0].0, Severity::Error);
    assert_eq!(found[0].1, Some(2));
}

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
fn zero_consumers_on_both_queues_warns_with_both_locations() {
    // Arrange
    let text = "[queue]\nprimary_queue_consumers = 0\nsecondary_queue_consumers = 0\n";

    // Act
    let loaded = config::from_text("inengine.toml", text);

    // Assert
    let issue = match loaded.report.issues() {
        [issue] => issue,
        other => panic!("expected one issue, found {other:#?}"),
    };
    assert_eq!(issue.severity, Severity::Warning);
    assert_eq!(issue.related.len(), 1);
    let line_of = |span: Span| {
        loaded
            .sources
            .get(span.source)
            .map(|source| source.line_column(span.start).0)
    };
    assert_eq!(issue.span.and_then(line_of), Some(2));
    assert_eq!(line_of(issue.related[0].0), Some(3));
    assert!(loaded.settings.is_some());
}

#[test]
fn a_missing_path_entry_warns_and_an_empty_one_fails() {
    // Arrange
    let text =
        "[plugins]\ngone = \"/nonexistent/inengine/plugin\"\n\n[exec_whitelist]\nblank = \" \"\n";

    // Act
    let loaded = config::from_text("inengine.toml", text);

    // Assert
    let found = issues(&loaded);
    assert_eq!(found.len(), 2, "{found:#?}");
    assert_eq!(found[0].0, Severity::Warning);
    assert_eq!(found[0].1, Some(2));
    assert!(found[0].2.contains("does not exist"));
    assert_eq!(found[1].0, Severity::Error);
    assert_eq!(found[1].1, Some(5));
    assert!(found[1].2.contains("empty path"));
    assert!(loaded.settings.is_none());
}

#[test]
fn settings_load_returns_the_rendered_diagnostics_for_an_invalid_file() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let path = dir.path().join("inengine.toml");
    std::fs::write(&path, "[mail]\nport = 70000\n").expect("write configuration");

    // Act
    let result = Settings::load(path.to_str().expect("utf-8 path"));

    // Assert
    match result {
        Err(ConfigError::Invalid { diagnostics }) => {
            assert!(
                diagnostics.contains("inengine.toml:2:8: error"),
                "{diagnostics}"
            );
            assert!(
                diagnostics.contains("port must be at most 65535"),
                "{diagnostics}"
            );
        }
        other => panic!("expected Invalid, got {other:?}"),
    }
}

#[test]
fn settings_load_reads_a_valid_file() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let path = dir.path().join("inengine.toml");
    std::fs::write(&path, "[queue]\nqueue_driver = \"redis\"\n").expect("write configuration");

    // Act
    let settings = Settings::load(path.to_str().expect("utf-8 path")).expect("settings load");

    // Assert
    assert_eq!(settings.queue.queue_driver, QueueDriver::Redis);
    assert_eq!(
        settings.queue.primary_queue_consumers,
        defaults().queue.primary_queue_consumers
    );
}

#[test]
fn load_of_a_missing_file_returns_file_not_found() {
    // Arrange
    let path = Path::new("/nonexistent/path/inengine.toml");

    // Act
    let result = config::load(path);

    // Assert
    assert!(
        matches!(result, Err(ConfigError::FileNotFound { ref path }) if path == "/nonexistent/path/inengine.toml"),
        "expected FileNotFound, got: {:?}",
        result.err()
    );
}

#[test]
fn load_of_a_directory_returns_an_io_error() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");

    // Act
    let result = config::load(dir.path());

    // Assert
    assert!(
        matches!(result, Err(ConfigError::Io(_))),
        "expected Io, got: {:?}",
        result.err()
    );
}

#[test]
fn an_out_of_range_retry_count_is_rejected_at_its_line() {
    // Arrange
    let cases = [
        ("too_many.toml", "[queue]\nmax_retries = 101\n", 2),
        ("negative.toml", "[queue]\n\nmax_retries = -1\n", 3),
    ];

    // Act
    let loaded: Vec<_> = cases
        .iter()
        .map(|(name, text, _)| config::from_text(name, text))
        .collect();

    // Assert
    for (loaded, (_, _, line)) in loaded.iter().zip(cases) {
        assert!(loaded.settings.is_none());
        let found = issues(loaded);
        assert_eq!(found.len(), 1, "{found:#?}");
        assert_eq!(found[0].0, Severity::Error);
        assert_eq!(found[0].1, Some(line));
        assert!(found[0].2.contains("max_retries"), "{}", found[0].2);
    }
}
