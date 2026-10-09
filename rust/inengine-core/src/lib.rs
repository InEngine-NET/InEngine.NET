/// The plugin name under which the built-in commands are queued. Configuration rejects a
/// plugin with this name.
pub const BUILTIN_PLUGIN_NAME: &str = "inengine-core";

pub mod command;
pub mod commands;
pub mod config;
pub mod error;
pub mod output;
pub mod plugin;
pub mod queue;
pub mod queue_commands;
pub mod scheduling;
pub mod server;
