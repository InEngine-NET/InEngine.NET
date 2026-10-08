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

    pub fn make_ctx() -> CommandContext {
        let settings: Settings = toml::from_str("").unwrap();
        CommandContext {
            output: Output::new(false),
            settings: Arc::new(settings),
        }
    }

    pub fn make_ctx_with_whitelist(whitelist: HashMap<String, String>) -> CommandContext {
        let mut settings: Settings = toml::from_str("").unwrap();
        settings.exec_whitelist = whitelist;
        CommandContext {
            output: Output::new(false),
            settings: Arc::new(settings),
        }
    }
}
