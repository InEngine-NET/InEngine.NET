use std::path::{Path, PathBuf};
use std::sync::Arc;

use tracing::{debug, error, info};

use crate::error::PluginError;

wasmtime::component::bindgen!({
    path: "../sample-plugin/wit",
    world: "plugin",
});

pub struct CommandDescriptor {
    pub name: String,
    pub help_text: String,
}

pub struct ScheduleEntry {
    pub command_name: String,
    pub cron_expression: String,
}

pub struct LoadedPlugin {
    pub name: String,
    pub path: PathBuf,
    pub component: Arc<wasmtime::component::Component>,
    pub command_descriptors: Vec<CommandDescriptor>,
    pub schedule_entries: Vec<ScheduleEntry>,
}

pub struct PluginLoader {
    engine: Arc<wasmtime::Engine>,
    pub plugins: Vec<LoadedPlugin>,
}

impl PluginLoader {
    pub fn new() -> Result<Self, PluginError> {
        let mut config = wasmtime::Config::new();
        config.wasm_component_model(true);
        let engine = wasmtime::Engine::new(&config).map_err(|e| PluginError::Load {
            name: "engine".to_string(),
            reason: e.to_string(),
        })?;
        Ok(Self {
            engine: Arc::new(engine),
            plugins: Vec::new(),
        })
    }

    pub fn load_from_directories(
        &mut self,
        plugin_dirs: &std::collections::HashMap<String, String>,
    ) -> Result<(), PluginError> {
        for (plugin_name, dir_path) in plugin_dirs {
            let dir = Path::new(dir_path);
            if !dir.exists() {
                debug!("plugin directory does not exist: {dir_path}");
                continue;
            }
            match self.load_directory(plugin_name, dir) {
                Ok(()) => {}
                Err(e) => error!("failed to load plugin {plugin_name}: {e}"),
            }
        }
        Ok(())
    }

    fn load_directory(
        &mut self,
        plugin_name: &str,
        dir: &Path,
    ) -> Result<(), PluginError> {
        let entries = std::fs::read_dir(dir).map_err(|e| PluginError::Load {
            name: plugin_name.to_string(),
            reason: e.to_string(),
        })?;

        for entry in entries.flatten() {
            let path = entry.path();
            if path.extension().and_then(|e| e.to_str()) == Some("wasm") {
                match self.load_component(plugin_name, &path) {
                    Ok(plugin) => {
                        info!(
                            "loaded plugin '{}' with {} commands",
                            plugin.name,
                            plugin.command_descriptors.len()
                        );
                        self.plugins.push(plugin);
                    }
                    Err(e) => {
                        error!("failed to load {}: {e}", path.display());
                    }
                }
            }
        }
        Ok(())
    }

    fn load_component(
        &self,
        plugin_name: &str,
        path: &Path,
    ) -> Result<LoadedPlugin, PluginError> {
        let component =
            wasmtime::component::Component::from_file(&self.engine, path).map_err(|e| {
                PluginError::Load {
                    name: plugin_name.to_string(),
                    reason: e.to_string(),
                }
            })?;

        let mut store = wasmtime::Store::new(&self.engine, ());
        let linker = wasmtime::component::Linker::new(&self.engine);

        let instance = Plugin::instantiate(&mut store, &component, &linker).map_err(|e| {
            PluginError::Load {
                name: plugin_name.to_string(),
                reason: e.to_string(),
            }
        })?;

        let commands_iface = instance.inengine_plugin_commands();
        let raw_descriptors =
            commands_iface
                .call_list_commands(&mut store)
                .map_err(|e| PluginError::Load {
                    name: plugin_name.to_string(),
                    reason: e.to_string(),
                })?;

        let command_descriptors = raw_descriptors
            .into_iter()
            .map(|d| CommandDescriptor {
                name: d.name,
                help_text: d.help_text,
            })
            .collect();

        let scheduling_iface = instance.inengine_plugin_scheduling();
        let raw_schedules =
            scheduling_iface
                .call_list_schedules(&mut store)
                .map_err(|e| PluginError::Load {
                    name: plugin_name.to_string(),
                    reason: e.to_string(),
                })?;

        let schedule_entries = raw_schedules
            .into_iter()
            .map(|s| ScheduleEntry {
                command_name: s.command_name,
                cron_expression: s.cron_expression,
            })
            .collect();

        Ok(LoadedPlugin {
            name: plugin_name.to_string(),
            path: path.to_path_buf(),
            component: Arc::new(component),
            command_descriptors,
            schedule_entries,
        })
    }

    pub fn engine(&self) -> &Arc<wasmtime::Engine> {
        &self.engine
    }
}

impl Default for PluginLoader {
    fn default() -> Self {
        Self::new().expect("failed to create default plugin loader")
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::collections::HashMap;

    #[test]
    fn load_from_empty_directory_returns_zero_plugins() {
        // Arrange
        let temp_dir = tempfile::tempdir().unwrap();
        let mut loader = PluginLoader::new().unwrap();
        let mut dirs = HashMap::new();
        dirs.insert("test".to_string(), temp_dir.path().to_str().unwrap().to_string());

        // Act
        loader.load_from_directories(&dirs).unwrap();

        // Assert
        assert!(loader.plugins.is_empty());
    }

    #[test]
    fn load_from_nonexistent_directory_does_not_error() {
        // Arrange
        let mut loader = PluginLoader::new().unwrap();
        let mut dirs = HashMap::new();
        dirs.insert("test".to_string(), "/nonexistent/path".to_string());

        // Act
        let result = loader.load_from_directories(&dirs);

        // Assert
        assert!(result.is_ok());
        assert!(loader.plugins.is_empty());
    }

    #[test]
    fn load_invalid_wasm_file_does_not_panic() {
        // Arrange
        let temp_dir = tempfile::tempdir().unwrap();
        let wasm_path = temp_dir.path().join("invalid.wasm");
        std::fs::write(&wasm_path, b"not a wasm file").unwrap();

        let mut loader = PluginLoader::new().unwrap();
        let mut dirs = HashMap::new();
        dirs.insert("test".to_string(), temp_dir.path().to_str().unwrap().to_string());

        // Act
        loader.load_from_directories(&dirs).unwrap();

        // Assert
        assert!(loader.plugins.is_empty());
    }

    #[test]
    fn plugin_error_load_failed_has_correct_fields() {
        // Arrange
        let err = PluginError::Load {
            name: "my-plugin".to_string(),
            reason: "file not found".to_string(),
        };

        // Act
        let display = format!("{err}");

        // Assert
        assert!(display.contains("my-plugin"));
        assert!(display.contains("file not found"));
    }

    #[test]
    fn plugin_error_command_not_found_has_correct_fields() {
        // Arrange
        let err = PluginError::CommandNotFound {
            plugin: "my-plugin".to_string(),
            command: "unknown".to_string(),
        };

        // Act
        let display = format!("{err}");

        // Assert
        assert!(display.contains("my-plugin"));
        assert!(display.contains("unknown"));
    }

    #[test]
    fn plugin_error_runtime_error_has_correct_fields() {
        // Arrange
        let err = PluginError::Runtime {
            plugin: "my-plugin".to_string(),
            message: "something broke".to_string(),
        };

        // Act
        let display = format!("{err}");

        // Assert
        assert!(display.contains("my-plugin"));
        assert!(display.contains("something broke"));
    }
}
