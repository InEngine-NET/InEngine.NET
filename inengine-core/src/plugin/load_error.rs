use wasmtime::Trap;

use crate::plugin::limiter::MemoryLimitExceeded;
use crate::plugin::loader::PLUGIN_LOAD_TIMEOUT;
use crate::scheduling::schedule::ScheduleError;

/// Why a plugin directory or component did not load.
#[derive(Debug, thiserror::Error)]
pub enum LoadError {
    /// The configured plugin directory does not exist or could not be listed.
    #[error("could not read the plugin directory")]
    ReadDirectory(#[source] std::io::Error),

    /// A `.wasm` file in the plugin directory could not be read.
    #[error("could not read the component file")]
    ReadComponent(#[source] std::io::Error),

    /// A `.wasm` file is not a valid WebAssembly component.
    #[error("could not compile the component")]
    Compile(#[source] wasmtime::Error),

    /// Instantiation, or the calls that list commands and schedules, ran past
    /// `PLUGIN_LOAD_TIMEOUT`.
    #[error("the plugin did not load within {seconds} seconds")]
    TimedOut {
        /// The load time limit that was exceeded, in seconds.
        seconds: u64,
    },

    /// The component tried to grow its memory past the plugin memory budget while it loaded.
    #[error(transparent)]
    MemoryLimit(#[from] MemoryLimitExceeded),

    /// The component trapped or does not match the plugin interface, for example because it
    /// imports something the host does not provide.
    #[error("could not instantiate the component or list its commands and schedules")]
    Instantiate(#[source] wasmtime::Error),
}

impl LoadError {
    /// A trap from the load deadline is a timeout, and a memory limit error keeps its type.
    pub(super) fn from_load_call(error: wasmtime::Error) -> Self {
        if error.downcast_ref::<Trap>() == Some(&Trap::Interrupt) {
            return LoadError::TimedOut {
                seconds: PLUGIN_LOAD_TIMEOUT.as_secs(),
            };
        }
        match error.downcast::<MemoryLimitExceeded>() {
            Ok(exceeded) => LoadError::MemoryLimit(exceeded),
            Err(error) => LoadError::Instantiate(error),
        }
    }
}

/// Why a schedule entry of a plugin was skipped.
#[derive(Debug, thiserror::Error)]
pub enum ScheduleEntryError {
    /// The entry refers to a command that the same component does not list.
    #[error("the plugin does not list this command")]
    NotListed,

    /// The cron expression of the entry could not be parsed.
    #[error(transparent)]
    InvalidCron(#[from] ScheduleError),
}
