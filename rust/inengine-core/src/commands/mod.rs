pub mod chain;
pub mod echo;
pub mod exec;

#[cfg(test)]
pub(crate) mod test_helpers {
    use std::collections::HashMap;
    use std::sync::Arc;

    use crate::command::CommandContext;
    use crate::config::Settings;
    use crate::output::Output;

    fn default_settings() -> Settings {
        crate::config::from_spec(&crate::config::SettingsSpec::default())
            .settings
            .expect("the default settings are valid")
    }

    pub fn make_ctx() -> CommandContext {
        let settings = default_settings();
        CommandContext {
            output: Output::new(false),
            settings: Arc::new(settings),
        }
    }

    pub fn make_ctx_with_whitelist(whitelist: HashMap<String, String>) -> CommandContext {
        let mut settings = default_settings();
        settings.exec_whitelist = whitelist;
        CommandContext {
            output: Output::new(false),
            settings: Arc::new(settings),
        }
    }
}
