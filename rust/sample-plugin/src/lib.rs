wit_bindgen::generate!({
    path: "wit",
    world: "plugin",
});

struct SamplePlugin;

export!(SamplePlugin);

impl exports::inengine::plugin::commands::Guest for SamplePlugin {
    fn list_commands() -> Vec<exports::inengine::plugin::commands::CommandDescriptor> {
        vec![
            exports::inengine::plugin::commands::CommandDescriptor {
                name: "greet".to_string(),
                help_text: "Print a greeting".to_string(),
            },
            exports::inengine::plugin::commands::CommandDescriptor {
                name: "version".to_string(),
                help_text: "Print the plugin version".to_string(),
            },
        ]
    }

    fn run_command(name: String, args_json: String) -> Result<String, String> {
        match name.as_str() {
            "greet" => {
                let who = serde_json::from_str::<serde_json::Value>(&args_json)
                    .ok()
                    .and_then(|v| v.get("name").and_then(|n| n.as_str()).map(String::from))
                    .unwrap_or_else(|| "world".to_string());
                Ok(format!("Hello, {who}!"))
            }
            "version" => Ok("sample-plugin 0.1.0".to_string()),
            _ => Err(format!("unknown command: {name}")),
        }
    }
}

impl exports::inengine::plugin::scheduling::Guest for SamplePlugin {
    fn list_schedules() -> Vec<exports::inengine::plugin::scheduling::ScheduleEntry> {
        vec![]
    }
}
