use std::time::Instant;

use wasmtime::Trap;
use wasmtime::component::{Component, Linker};

use crate::command::{Command, CommandContext, CommandDeadline, CommandResult, instant_after};
use crate::error::CommandError;
use crate::plugin::limiter::MemoryLimitExceeded;
use crate::plugin::loader::Plugin;
use crate::plugin::runtime::{InterruptCause, PluginRuntime, PluginStoreData};

/// Runs one command of a loaded plugin in a new sandboxed instance.
pub(crate) struct WasmCommand {
    plugin_name: String,
    command_name: String,
    args_json: String,
    component: Component,
    runtime: PluginRuntime,
}

/// Which deadline a call received, so an interrupt at that deadline reports the right limit.
#[derive(Debug, Clone, Copy)]
enum CallDeadline {
    Context { seconds: u64 },
    PluginLimit { seconds: u64 },
}

impl WasmCommand {
    pub(crate) fn new(
        plugin_name: &str,
        component: &Component,
        runtime: PluginRuntime,
        command_name: &str,
        args_json: &str,
    ) -> Self {
        Self {
            plugin_name: plugin_name.to_string(),
            command_name: command_name.to_string(),
            args_json: args_json.to_string(),
            component: component.clone(),
            runtime,
        }
    }

    fn call_deadline(&self, context_deadline: Option<CommandDeadline>) -> (Instant, CallDeadline) {
        let limit = self.runtime.limits().timeout;
        let limit_deadline = instant_after(limit);
        match context_deadline {
            Some(deadline) if deadline.at < limit_deadline => (
                deadline.at,
                CallDeadline::Context {
                    seconds: deadline.timeout.as_secs(),
                },
            ),
            _ => (
                limit_deadline,
                CallDeadline::PluginLimit {
                    seconds: limit.as_secs(),
                },
            ),
        }
    }

    fn classify_call_failure(
        &self,
        error: wasmtime::Error,
        store_data: &PluginStoreData,
        deadline: CallDeadline,
    ) -> CommandError {
        let (plugin, command) = (&self.plugin_name, &self.command_name);
        if error.downcast_ref::<Trap>() == Some(&Trap::Interrupt) {
            match (store_data.interrupt_cause(), deadline) {
                (Some(InterruptCause::Shutdown), _) => return CommandError::Interrupted,
                (Some(InterruptCause::Deadline), CallDeadline::Context { seconds }) => {
                    return CommandError::Timeout { seconds };
                }
                (Some(InterruptCause::Deadline), CallDeadline::PluginLimit { seconds }) => {
                    return CommandError::failed(format!(
                        "command '{command}' in plugin '{plugin}' exceeded the plugin time limit of {seconds} seconds"
                    ));
                }
                (None, _) => {}
            }
        }
        match error.downcast::<MemoryLimitExceeded>() {
            Ok(exceeded) => CommandError::failed_with(
                format!("command '{command}' in plugin '{plugin}' ran out of memory"),
                exceeded,
            ),
            Err(error) => CommandError::failed_with(
                format!("runtime error in command '{command}' in plugin '{plugin}'"),
                error,
            ),
        }
    }
}

impl Command for WasmCommand {
    fn name(&self) -> &str {
        &self.command_name
    }

    fn run(&self, ctx: &mut CommandContext) -> CommandResult {
        let (deadline, call_deadline) = self.call_deadline(ctx.deadline);
        let mut store = self.runtime.new_store(deadline, ctx.cancel.clone());
        let linker = Linker::new(self.runtime.engine());
        let called =
            Plugin::instantiate(&mut store, &self.component, &linker).and_then(|instance| {
                instance.inengine_plugin_commands().call_run_command(
                    &mut store,
                    &self.command_name,
                    &self.args_json,
                )
            });
        match called {
            Ok(Ok(output)) => {
                if !output.is_empty() {
                    ctx.output.line(&output);
                }
                Ok(())
            }
            Ok(Err(message)) => Err(CommandError::failed(format!(
                "command '{}' in plugin '{}' failed: {message}",
                self.command_name, self.plugin_name
            ))),
            Err(error) => Err(self.classify_call_failure(error, store.data(), call_deadline)),
        }
    }
}
