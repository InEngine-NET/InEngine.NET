use std::sync::Arc;

use crate::command::{Command, CommandContext, CommandResult};
use crate::error::CommandError;
use crate::plugin::loader::Plugin;

pub struct WasmCommand {
    pub plugin_name: String,
    pub command_name: String,
    pub args_json: String,
    pub component: Arc<wasmtime::component::Component>,
    pub engine: Arc<wasmtime::Engine>,
}

impl Command for WasmCommand {
    fn name(&self) -> &str {
        &self.command_name
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let mut store = wasmtime::Store::new(&self.engine, ());
        let linker = wasmtime::component::Linker::new(&self.engine);

        let instance =
            Plugin::instantiate(&mut store, &self.component, &linker).map_err(|e| {
                CommandError::Failed {
                    message: format!(
                        "failed to instantiate plugin '{}' for command '{}': {e}",
                        self.plugin_name, self.command_name
                    ),
                }
            })?;

        let commands_iface = instance.inengine_plugin_commands();
        let result = commands_iface
            .call_run_command(&mut store, &self.command_name, &self.args_json)
            .map_err(|e| CommandError::Failed {
                message: format!(
                    "runtime error calling '{}' in plugin '{}': {e}",
                    self.command_name, self.plugin_name
                ),
            })?;

        match result {
            Ok(output) => {
                if !output.is_empty() {
                    ctx.output.line(&output);
                }
                Ok(())
            }
            Err(err_msg) => Err(CommandError::Failed {
                message: format!(
                    "command '{}' in plugin '{}' failed: {err_msg}",
                    self.command_name, self.plugin_name
                ),
            }),
        }
    }
}
