//! The InEngine engine. It runs commands now, on a cron schedule, or from a queue, and it
//! loads more commands from WebAssembly plugins. The `inengine` program is built on it.

/// The plugin name under which the built-in commands are queued. Configuration rejects a
/// plugin with this name.
pub const BUILTIN_PLUGIN_NAME: &str = "inengine-core";

/// The `Command` trait that every unit of work implements, and the context it runs with.
pub mod command;
/// The built-in commands: `echo`, `exec`, and `chain`.
pub mod commands;
/// Loads and validates the configuration file into `Settings`.
pub mod config;
/// The error types that commands, configuration, queues, and plugins return.
pub mod error;
/// A text buffer that commands write to, with optional terminal colors and a progress bar.
pub mod output;
/// Loads WebAssembly plugin components and runs their commands within time and memory limits.
pub mod plugin;
/// Queues of command messages on Redis or on files, and the registry that turns a message into
/// a command.
pub mod queue;
pub mod queue_admin;
/// Cron schedules of commands that the server runs.
pub mod scheduling;
/// The long running server that runs scheduled jobs and consumes the queues until you stop it.
pub mod server;
