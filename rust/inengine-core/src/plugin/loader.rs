use std::collections::HashMap;
use std::path::{Path, PathBuf};
use std::sync::Arc;
use std::time::{Duration, Instant};

use tokio_util::sync::CancellationToken;
use wasmtime::component::{Component, Linker};

use crate::config::Settings;
use crate::error::PluginError;
use crate::plugin::load_error::{LoadError, ScheduleEntryError};
use crate::plugin::runtime::PluginRuntime;
use crate::plugin::wasm_command::WasmCommand;
use crate::queue::CommandRegistry;
use crate::scheduling::schedule::Schedule;

wasmtime::component::bindgen!({
    path: "wit",
    world: "plugin",
});

/// The deadline for instantiating a component and listing its commands and schedules.
pub const PLUGIN_LOAD_TIMEOUT: Duration = Duration::from_secs(10);

/// A command that a plugin lists.
pub struct CommandDescriptor {
    /// The command name that a queued message or a schedule entry uses to select the command.
    pub name: String,
    /// A description of the command that the plugin supplies for people.
    pub help_text: String,
}

/// A schedule entry that a plugin lists.
pub struct ScheduleEntry {
    /// The command to run, which must be one that the same component lists.
    pub command_name: String,
    /// When to run the command, in the format of the `cron` crate, which starts with a seconds
    /// field.
    pub cron_expression: String,
}

/// One component that loaded, with what it lists.
pub struct LoadedPlugin {
    /// The plugin name from the configuration, which every command of the component uses.
    pub name: String,
    /// The path of the `.wasm` file the component was loaded from.
    pub path: PathBuf,
    pub(crate) component: Component,
    /// The commands that the component lists, in the order it lists them.
    pub command_descriptors: Vec<CommandDescriptor>,
    /// The schedule entries that the component lists. Entries are checked only when they are
    /// registered.
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
        directories: &HashMap<String, PathBuf>,
        runtime: PluginRuntime,
    ) -> (PluginLoader, Vec<PluginError>) {
        let mut loader = PluginLoader {
            runtime,
            plugins: Vec::new(),
        };
        let mut errors = Vec::new();
        for (plugin_name, directory) in directories {
            loader.load_directory(plugin_name, directory, &mut errors);
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
                let invalid_entry =
                    |source: ScheduleEntryError| PluginError::InvalidScheduleEntry {
                        plugin: plugin.name.clone(),
                        command: entry.command_name.clone(),
                        source,
                    };
                if !plugin.lists_command(&entry.command_name) {
                    errors.push(invalid_entry(ScheduleEntryError::NotListed));
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
                    Err(error) => errors.push(invalid_entry(error.into())),
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
                    directory,
                    LoadError::ReadDirectory(error),
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
        let load_failure = |source: LoadError| load_error(plugin_name, path, source);
        let bytes =
            std::fs::read(path).map_err(|error| load_failure(LoadError::ReadComponent(error)))?;
        let component = Component::new(self.runtime.engine(), &bytes)
            .map_err(|error| load_failure(LoadError::Compile(error)))?;
        let mut store = self.runtime.new_store(
            Instant::now() + PLUGIN_LOAD_TIMEOUT,
            CancellationToken::new(),
        );
        let linker = Linker::new(self.runtime.engine());
        let instance = Plugin::instantiate(&mut store, &component, &linker)
            .map_err(|error| load_failure(LoadError::from_load_call(error)))?;
        let command_descriptors = instance
            .inengine_plugin_commands()
            .call_list_commands(&mut store)
            .map_err(|error| load_failure(LoadError::from_load_call(error)))?
            .into_iter()
            .map(|descriptor| CommandDescriptor {
                name: descriptor.name,
                help_text: descriptor.help_text,
            })
            .collect();
        let schedule_entries = instance
            .inengine_plugin_scheduling()
            .call_list_schedules(&mut store)
            .map_err(|error| load_failure(LoadError::from_load_call(error)))?
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

fn load_error(plugin_name: &str, path: &Path, source: LoadError) -> PluginError {
    PluginError::Load {
        name: plugin_name.to_string(),
        path: path.to_path_buf(),
        source,
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::config::PluginLimitsSettings;
    use crate::error::ErrorChain;
    use crate::scheduling::schedule::ScheduleError;

    fn runtime() -> PluginRuntime {
        PluginRuntime::new(PluginLimitsSettings::default()).unwrap()
    }

    fn directories(name: &str, path: &Path) -> HashMap<String, PathBuf> {
        HashMap::from([(name.to_string(), path.to_path_buf())])
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
        assert!(matches!(&errors[0], PluginError::Load {
            name,
            path,
            source: LoadError::ReadDirectory(_),
        } if name == "test" && path == missing));
    }

    #[test]
    fn a_plugin_load_error_keeps_its_source() {
        // Arrange
        let missing = Path::new("/nonexistent/path");

        // Act
        let (_loader, errors) = PluginLoader::load(&directories("test", missing), runtime());

        // Assert
        let source = std::error::Error::source(&errors[0]).expect("the error keeps its source");
        let load_error = source
            .downcast_ref::<LoadError>()
            .expect("the source is a LoadError");
        assert!(
            matches!(load_error, LoadError::ReadDirectory(io) if io.kind() == std::io::ErrorKind::NotFound),
            "{load_error:?}"
        );
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
        assert!(matches!(&errors[0], PluginError::Load {
            name,
            path,
            source: LoadError::Compile(_),
        } if name == "test" && path == &temp_dir.path().join("invalid.wasm")));
    }

    fn fixture_schedule_errors() -> Vec<PluginError> {
        let fixture = Path::new(env!("CARGO_MANIFEST_DIR")).join("tests/fixtures/plugin");
        let (loader, errors) = PluginLoader::load(&directories("fixture", &fixture), runtime());
        assert!(errors.is_empty(), "{errors:?}");
        loader.register_schedules(&mut Schedule::new())
    }

    #[test]
    fn an_unlisted_schedule_entry_reports_not_listed() {
        // Arrange
        let expected = "invalid schedule entry for command 'missing' in plugin 'fixture': \
                        the plugin does not list this command";

        // Act
        let errors = fixture_schedule_errors();

        // Assert
        assert!(matches!(&errors[0], PluginError::InvalidScheduleEntry {
            plugin,
            command,
            source: ScheduleEntryError::NotListed,
        } if plugin == "fixture" && command == "missing"));
        assert_eq!(ErrorChain::new(&errors[0]).to_string(), expected);
    }

    #[test]
    fn an_invalid_cron_entry_keeps_the_schedule_error() {
        // Arrange
        let expected_prefix = "invalid schedule entry for command 'echo-args' in plugin 'fixture': \
                               invalid cron expression 'not a cron'";

        // Act
        let errors = fixture_schedule_errors();

        // Assert
        assert_eq!(errors.len(), 2);
        assert!(matches!(&errors[1], PluginError::InvalidScheduleEntry {
            source: ScheduleEntryError::InvalidCron(ScheduleError::InvalidCron { expression, .. }),
            ..
        } if expression == "not a cron"));
        let chain = ErrorChain::new(&errors[1]).to_string();
        assert!(chain.starts_with(expected_prefix), "{chain}");
    }
}
