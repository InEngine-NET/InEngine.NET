//! Loads `inengine.toml` into [`Settings`].
//!
//! The file is parsed into span-tracked spec types, validated so that every
//! problem is reported in one pass, and lowered into the runtime types only
//! when the report holds no errors.

mod constraints;
mod runtime;
mod spec;
mod validate;
mod vocabulary;

use std::path::Path;

use confval::prelude::*;

use crate::error::ConfigError;

pub use runtime::{FileSettings, MailSettings, QueueSettings, RedisSettings, Settings};
pub use spec::{FileSpec, MailSpec, QueueSpec, RedisSpec, SettingsSpec};
pub use vocabulary::QueueDriver;

/// The result of a configuration load.
///
/// `settings` is `None` when the report holds an error. The report can hold
/// warnings in both cases, so render it whenever it holds any issue.
pub struct Loaded {
    pub settings: Option<Settings>,
    pub report: Report,
    pub sources: SourceMap,
}

impl Loaded {
    /// Renders the report without color, for logs and non-terminal output.
    pub fn render_plain(&self) -> String {
        let mut out = String::new();
        match self.report.render_plain(&self.sources, &mut out) {
            Ok(()) => out,
            Err(error) => format!("could not render the configuration report: {error}"),
        }
    }

    /// Renders the report with color and source snippets, for a terminal.
    pub fn render_pretty(&self) -> String {
        let mut out = String::new();
        match self.report.render_pretty(&self.sources, &mut out) {
            Ok(()) => out,
            Err(error) => format!("could not render the configuration report: {error}"),
        }
    }
}

/// Reads and checks the configuration file at `path`.
///
/// A missing or unreadable file is an `Err`. Every problem inside the file is
/// in the returned report.
pub fn load(path: &Path) -> Result<Loaded, ConfigError> {
    let text = std::fs::read_to_string(path).map_err(|e| {
        if e.kind() == std::io::ErrorKind::NotFound {
            ConfigError::FileNotFound {
                path: path.display().to_string(),
            }
        } else {
            ConfigError::Io(e)
        }
    })?;
    Ok(from_text(&path.display().to_string(), &text))
}

/// Parses and checks configuration text. `name` labels the text in diagnostics.
pub fn from_text(name: &str, text: &str) -> Loaded {
    let mut sources = SourceMap::new();
    let mut report = Report::new();
    let id = sources.add(name, text);
    let spec: Option<SettingsSpec> = confval::format::toml::parse_toml(&sources, id, &mut report);
    let settings = spec.and_then(|spec| check_and_lower(&spec, &mut report));
    Loaded {
        settings,
        report,
        sources,
    }
}

/// Checks a spec built in code and lowers it.
///
/// `SettingsSpec::default()` gives the same settings as an empty file.
pub fn from_spec(spec: &SettingsSpec) -> Loaded {
    let mut report = Report::new();
    let settings = check_and_lower(spec, &mut report);
    Loaded {
        settings,
        report,
        sources: SourceMap::new(),
    }
}

fn check_and_lower(spec: &SettingsSpec, report: &mut Report) -> Option<Settings> {
    spec.validate_all(report);
    if report.has_errors() {
        return None;
    }
    Settings::lower(spec, report)
}

impl Settings {
    /// Loads the configuration file at `path`, discarding any warnings.
    ///
    /// Use [`load`] to receive the warnings and the source map for rendering.
    pub fn load(path: &str) -> Result<Settings, ConfigError> {
        let loaded = load(Path::new(path))?;
        match loaded.settings {
            Some(settings) => Ok(settings),
            None => Err(ConfigError::Invalid {
                diagnostics: loaded.render_plain(),
            }),
        }
    }
}

#[cfg(test)]
pub(crate) fn settings_with_queue_base(base: &Path) -> Settings {
    let mut settings = from_spec(&SettingsSpec::default())
        .settings
        .expect("the default settings are valid");
    settings.queue.file.base_path = base.to_str().expect("utf-8 path").to_string();
    settings
}
