pub mod chain;
pub mod echo;
pub mod exec;

#[cfg(test)]
pub(crate) mod test_helpers {
    use std::collections::HashMap;
    use std::sync::Arc;

    use crate::command::CommandContext;
    use crate::config::ConfigFile;
    use crate::output::Output;

    pub fn make_ctx() -> CommandContext {
        let config: ConfigFile = serde_json::from_str(r#"{ "InEngine": {} }"#).unwrap();
        CommandContext {
            output: Output::new(false),
            settings: Arc::new(config.inengine),
        }
    }

    pub fn make_ctx_with_whitelist(whitelist: HashMap<String, String>) -> CommandContext {
        let config: ConfigFile = serde_json::from_str(r#"{ "InEngine": {} }"#).unwrap();
        let mut settings = config.inengine;
        settings.exec_whitelist = whitelist;
        CommandContext {
            output: Output::new(false),
            settings: Arc::new(settings),
        }
    }
}
