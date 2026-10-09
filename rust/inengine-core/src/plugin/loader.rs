use std::collections::HashMap;
use std::path::{Path, PathBuf};
use std::sync::Arc;
use std::time::{Duration, Instant};

use tokio_util::sync::CancellationToken;
use wasmtime::component::{Component, Linker};
use wasmtime::Trap;

use crate::config::Settings;
use crate::error::PluginError;
use crate::plugin::limiter::MemoryLimitExceeded;
use crate::plugin::runtime::PluginRuntime;
use crate::plugin::wasm_command::WasmCommand;
use crate::queue::registry::CommandRegistry;
use crate::scheduling::schedule::Schedule;

wasmtime::component::bindgen!({
    path: "../sample-plugin/wit",
    world: "plugin",
});

/// The deadline for instantiating a component and listing its commands and schedules.
pub const PLUGIN_LOAD_TIMEOUT: Duration = Duration::from_secs(10);

/// A command that a plugin lists.
pub struct CommandDescriptor {
    pub name: String,
    pub help_text: String,
}

/// A schedule entry that a plugin lists.
pub struct ScheduleEntry {
    pub command_name: String,
    pub cron_expression: String,
}

/// One component that loaded, with what it lists.
pub struct LoadedPlugin {
    pub name: String,
    pub path: PathBuf,
    pub(crate) component: Component,
    pub command_descriptors: Vec<CommandDescriptor>,
    pub schedule_entries: Vec<ScheduleEntry>,
}

impl LoadedPlugin {
    fn lists_command(&self, command_name: &str) -> bool {
        self.command_descriptors
            .iter()
            .any(|descriptor| descriptor.name == command_name)
    }
}

/// The plugins that loaded, and the runtime that runs their commands.
pub struct PluginLoader {
    runtime: PluginRuntime,
    plugins: Vec<LoadedPlugin>,
}

impl PluginLoader {
    /// Loads the plugins that the settings list, with the configured limits. Returns `None`
    /// when the settings list no plugins, so no runtime or ticker thread starts.
    pub fn from_settings(settings: &Settings) -> (Option<PluginLoader>, Vec<PluginError>) {
        if settings.plugins.is_empty() {
            return (None, Vec::new());
        }
        match PluginRuntime::new(settings.plugin_limits) {
            Ok(runtime) => {
                let (loader, errors) = PluginLoader::load(&settings.plugins, runtime);
                (Some(loader), errors)
            }
            Err(error) => (None, vec![error]),
        }
    }

    /// Loads every `.wasm` file in each directory under the directory's plugin name. A
    /// failure skips that directory or component and is returned with the others.
    pub fn load(
        directories: &HashMap<String, String>,
        runtime: PluginRuntime,
    ) -> (PluginLoader, Vec<PluginError>) {
        let mut loader = PluginLoader {
            runtime,
            plugins: Vec::new(),
        };
        let mut errors = Vec::new();
        for (plugin_name, directory) in directories {
            loader.load_directory(plugin_name, Path::new(directory), &mut errors);
        }
        (loader, errors)
    }

    /// The runtime that runs the commands of these plugins.
    pub fn runtime(&self) -> &PluginRuntime {
        &self.runtime
    }

    /// Each component that loaded, in load order.
    pub fn plugins(&self) -> &[LoadedPlugin] {
        &self.plugins
    }

    /// Registers each listed command under its plugin name. When two components of one
    /// plugin list the same command, the first stays and the second is returned as an error.
    pub fn register_commands(&self, registry: &mut CommandRegistry) -> Vec<PluginError> {
        let mut errors = Vec::new();
        for plugin in &self.plugins {
            for descriptor in &plugin.command_descriptors {
                let command_name = descriptor.name.clone();
                let plugin_name = plugin.name.clone();
                let component = plugin.component.clone();
                let runtime = self.runtime.clone();
                let registered = registry.register(&plugin.name, &descriptor.name, move |json| {
                    Ok(Box::new(WasmCommand::new(
                        &plugin_name,
                        &component,
                        runtime.clone(),
                        &command_name,
                        json,
                    )))
                });
                if let Err(duplicate) = registered {
                    errors.push(duplicate.into());
                }
            }
        }
        errors
    }

    /// Adds a job named `<plugin>::<command>` for each schedule entry. A job runs its command
    /// with the arguments `{}`, because a schedule entry has no arguments.
    pub fn register_schedules(&self, schedule: &mut Schedule) -> Vec<PluginError> {
        let mut errors = Vec::new();
        for plugin in &self.plugins {
            for entry in &plugin.schedule_entries {
                let invalid_entry = |reason: String| PluginError::InvalidScheduleEntry {
                    plugin: plugin.name.clone(),
                    command: entry.command_name.clone(),
                    reason,
                };
                if !plugin.lists_command(&entry.command_name) {
                    errors.push(invalid_entry(
                        "the plugin does not list this command".to_string(),
                    ));
                    continue;
                }
                let command = WasmCommand::new(
                    &plugin.name,
                    &plugin.component,
                    self.runtime.clone(),
                    &entry.command_name,
                    "{}",
                );
                let job = schedule
                    .command_with_name(
                        format!("{}::{}", plugin.name, entry.command_name),
                        Arc::new(command),
                    )
                    .cron(&entry.cron_expression);
                match job {
                    Ok(job) => schedule.add(job),
                    Err(error) => errors.push(invalid_entry(error.to_string())),
                }
            }
        }
        errors
    }

    fn load_directory(
        &mut self,
        plugin_name: &str,
        directory: &Path,
        errors: &mut Vec<PluginError>,
    ) {
        let entries = match std::fs::read_dir(directory) {
            Ok(entries) => entries,
            Err(error) => {
                errors.push(load_error(
                    plugin_name,
                    format!("cannot read {}: {error}", directory.display()),
                ));
                return;
            }
        };
        let mut paths: Vec<PathBuf> = entries
            .flatten()
            .map(|entry| entry.path())
            .filter(|path| {
                path.extension().and_then(|extension| extension.to_str()) == Some("wasm")
            })
            .collect();
        // Sorted so the first of two components that list the same command is stable.
        paths.sort();
        for path in paths {
            match self.load_component(plugin_name, &path) {
                Ok(plugin) => self.plugins.push(plugin),
                Err(error) => errors.push(error),
            }
        }
    }

    fn load_component(&self, plugin_name: &str, path: &Path) -> Result<LoadedPlugin, PluginError> {
        let load_failure =
            |reason: String| load_error(plugin_name, format!("{}: {reason}", path.display()));
        let component = Component::from_file(self.runtime.engine(), path)
            .map_err(|error| load_failure(error.to_string()))?;
        let mut store = self.runtime.new_store(
            Instant::now() + PLUGIN_LOAD_TIMEOUT,
            CancellationToken::new(),
        );
        let linker = Linker::new(self.runtime.engine());
        let instance = Plugin::instantiate(&mut store, &component, &linker)
            .map_err(|error| load_failure(describe_load_failure(&error)))?;
        let command_descriptors = instance
            .inengine_plugin_commands()
            .call_list_commands(&mut store)
            .map_err(|error| load_failure(describe_load_failure(&error)))?
            .into_iter()
            .map(|descriptor| CommandDescriptor {
                name: descriptor.name,
                help_text: descriptor.help_text,
            })
            .collect();
        let schedule_entries = instance
            .inengine_plugin_scheduling()
            .call_list_schedules(&mut store)
            .map_err(|error| load_failure(describe_load_failure(&error)))?
            .into_iter()
            .map(|entry| ScheduleEntry {
                command_name: entry.command_name,
                cron_expression: entry.cron_expression,
            })
            .collect();
        Ok(LoadedPlugin {
            name: plugin_name.to_string(),
            path: path.to_path_buf(),
            component,
            command_descriptors,
            schedule_entries,
        })
    }
}

fn describe_load_failure(error: &wasmtime::Error) -> String {
    if error.downcast_ref::<Trap>() == Some(&Trap::Interrupt) {
        return format!(
            "the plugin did not load within {} seconds",
            PLUGIN_LOAD_TIMEOUT.as_secs()
        );
    }
    if let Some(exceeded) = error.downcast_ref::<MemoryLimitExceeded>() {
        return exceeded.to_string();
    }
    error.to_string()
}

fn load_error(plugin_name: &str, reason: String) -> PluginError {
    PluginError::Load {
        name: plugin_name.to_string(),
        reason,
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::config::PluginLimitsSettings;

    fn runtime() -> PluginRuntime {
        PluginRuntime::new(PluginLimitsSettings::default()).unwrap()
    }

    fn directories(name: &str, path: &Path) -> HashMap<String, String> {
        HashMap::from([(name.to_string(), path.to_string_lossy().into_owned())])
    }

    #[test]
    fn empty_directory_loads_no_plugins_and_no_errors() {
        // Arrange
        let temp_dir = tempfile::tempdir().unwrap();

        // Act
        let (loader, errors) = PluginLoader::load(&directories("test", temp_dir.path()), runtime());

        // Assert
        assert!(loader.plugins().is_empty());
        assert!(errors.is_empty());
    }

    #[test]
    fn missing_directory_returns_one_load_error() {
        // Arrange
        let missing = Path::new("/nonexistent/path");

        // Act
        let (loader, errors) = PluginLoader::load(&directories("test", missing), runtime());

        // Assert
        assert!(loader.plugins().is_empty());
        assert_eq!(errors.len(), 1);
        assert!(matches!(&errors[0], PluginError::Load { name, reason }
            if name == "test" && reason.contains("/nonexistent/path")));
    }

    #[test]
    fn invalid_component_returns_one_load_error() {
        // Arrange
        let temp_dir = tempfile::tempdir().unwrap();
        std::fs::write(temp_dir.path().join("invalid.wasm"), b"not a wasm file").unwrap();

        // Act
        let (loader, errors) = PluginLoader::load(&directories("test", temp_dir.path()), runtime());

        // Assert
        assert!(loader.plugins().is_empty());
        assert_eq!(errors.len(), 1);
        assert!(matches!(&errors[0], PluginError::Load { name, reason }
            if name == "test" && reason.contains("invalid.wasm")));
    }

    #[test]
    fn invalid_schedule_entry_message_includes_the_plugin_the_command_and_the_reason() {
        // Arrange
        let error = PluginError::InvalidScheduleEntry {
            plugin: "my-plugin".to_string(),
            command: "run".to_string(),
            reason: "the plugin does not list this command".to_string(),
        };

        // Act
        let display = error.to_string();

        // Assert
        assert_eq!(
            display,
            "invalid schedule entry for command 'run' in plugin 'my-plugin': the plugin does not list this command"
        );
    }
}
