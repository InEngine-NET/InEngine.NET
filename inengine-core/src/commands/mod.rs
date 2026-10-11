/// A command that runs other commands in order.
pub mod chain;
/// A command that writes a line of text.
pub mod echo;
/// A command that starts a whitelisted executable.
pub mod exec;

#[cfg(test)]
pub(crate) mod test_helpers {
    use std::collections::HashMap;
    use std::path::PathBuf;
    use std::sync::Arc;

    use crate::command::CommandContext;
    use crate::config::Settings;
    use crate::output::Output;

    fn default_settings() -> Settings {
        crate::config::from_spec(&crate::config::SettingsSpec::default())
            .settings
            .expect("the default settings are valid")
    }

    pub(crate) fn make_ctx() -> CommandContext {
        let settings = default_settings();
        CommandContext::new(Output::new(false), Arc::new(settings))
    }

    pub(crate) fn make_ctx_with_whitelist(whitelist: HashMap<String, PathBuf>) -> CommandContext {
        let mut settings = default_settings();
        settings.exec_whitelist = whitelist;
        CommandContext::new(Output::new(false), Arc::new(settings))
    }
}
