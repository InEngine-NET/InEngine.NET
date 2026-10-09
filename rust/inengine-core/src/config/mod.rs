mod constraints;
mod runtime;
mod spec;
mod validate;
mod vocabulary;

use std::path::Path;

use confval::prelude::*;

use crate::error::ConfigError;

pub(crate) use runtime::BYTES_PER_MIB;
pub use runtime::{
    FileSettings, MailSettings, PluginLimitsSettings, QueueSettings, RedisSettings, Settings,
};
pub use spec::{FileSpec, MailSpec, PluginLimitsSpec, QueueSpec, RedisSpec, SettingsSpec};
pub use vocabulary::{MailEncryption, QueueDriver};

/// `settings` is `None` when the report holds an error. The report can hold
/// warnings in both cases, so render it whenever it holds any issue.
pub struct Loaded {
    /// The validated settings, or `None` when the report holds an error.
    pub settings: Option<Settings>,
    /// The errors and warnings found while the configuration was parsed and validated.
    pub report: Report,
    /// The configuration text that the report points into. The renderers use it to find the
    /// line and column of each issue.
    pub sources: SourceMap,
}

impl Loaded {
    /// Renders the report with one line per issue and no color, for logs and scripts.
    pub fn render_plain(&self) -> String {
        let mut out = String::new();
        match self.report.render_plain(&self.sources, &mut out) {
            Ok(()) => out,
            Err(error) => format!("could not render the configuration report: {error}"),
        }
    }

    /// Renders the report with color, a source excerpt under each issue, and help text, for a
    /// terminal.
    pub fn render_pretty(&self) -> String {
        let mut out = String::new();
        match self.report.render_pretty(&self.sources, &mut out) {
            Ok(()) => out,
            Err(error) => format!("could not render the configuration report: {error}"),
        }
    }
}

/// A missing or unreadable file is an `Err`. Every problem inside the file is
/// in the returned report.
pub fn load(path: &Path) -> Result<Loaded, ConfigError> {
    let text = std::fs::read_to_string(path).map_err(|e| {
        if e.kind() == std::io::ErrorKind::NotFound {
            ConfigError::FileNotFound {
                path: path.to_path_buf(),
            }
        } else {
            ConfigError::Io {
                path: path.to_path_buf(),
                source: e,
            }
        }
    })?;
    Ok(from_text(&path.display().to_string(), &text))
}

/// `name` labels the text in diagnostics.
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

/// Validates a spec that you built in code, for example in a test.
///
/// The returned value has an empty source map, so the report cannot show source lines.
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
    pub fn load(path: &Path) -> Result<Settings, ConfigError> {
        let loaded = load(path)?;
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
    settings.queue.file.base_path = base.to_path_buf();
    settings
}
