use std::thread::JoinHandle;
use std::time::{Duration, Instant};

use tokio_util::sync::CancellationToken;
use wasmtime::{Engine, EngineWeak, Store, UpdateDeadline};

use crate::config::PluginLimitsSettings;
use crate::error::PluginError;
use crate::plugin::limiter::PluginLimiter;

/// How often the ticker advances the engine epoch. A call stops at most this long after its
/// deadline.
pub(crate) const EPOCH_TICK_INTERVAL: Duration = Duration::from_millis(10);

/// The engine and limits that every plugin call shares. Clones share one engine and one
/// ticker thread.
#[derive(Clone)]
pub struct PluginRuntime {
    engine: Engine,
    limits: PluginLimitsSettings,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub(crate) enum InterruptCause {
    Deadline,
    Shutdown,
}

pub(crate) struct PluginStoreData {
    deadline: Instant,
    cancel: CancellationToken,
    interrupt_cause: Option<InterruptCause>,
    limiter: PluginLimiter,
}

impl PluginStoreData {
    pub(crate) fn interrupt_cause(&self) -> Option<InterruptCause> {
        self.interrupt_cause
    }
}

impl PluginRuntime {
    /// Starts the engine and its epoch ticker thread.
    pub fn new(limits: PluginLimitsSettings) -> Result<Self, PluginError> {
        let mut config = wasmtime::Config::new();
        config.wasm_component_model(true);
        config.epoch_interruption(true);
        let engine = Engine::new(&config).map_err(PluginError::Engine)?;
        start_epoch_ticker(engine.weak()).map_err(PluginError::Ticker)?;
        Ok(Self { engine, limits })
    }

    /// The limits that every call through this runtime runs with.
    pub fn limits(&self) -> PluginLimitsSettings {
        self.limits
    }

    pub(crate) fn engine(&self) -> &Engine {
        &self.engine
    }

    /// The only way to make a store. With epoch interruption on, a store without a deadline
    /// traps on its first instruction.
    pub(crate) fn new_store(
        &self,
        deadline: Instant,
        cancel: CancellationToken,
    ) -> Store<PluginStoreData> {
        let data = PluginStoreData {
            deadline,
            cancel,
            interrupt_cause: None,
            limiter: PluginLimiter::new(self.limits.memory_bytes),
        };
        let mut store = Store::new(&self.engine, data);
        store.limiter(|data| &mut data.limiter);
        store.set_epoch_deadline(1);
        // The deadline is wall time, so a ticker that oversleeps cannot lengthen a call.
        store.epoch_deadline_callback(|mut context| {
            let data = context.data_mut();
            let cause = if data.cancel.is_cancelled() {
                Some(InterruptCause::Shutdown)
            } else if Instant::now() >= data.deadline {
                Some(InterruptCause::Deadline)
            } else {
                None
            };
            match cause {
                Some(cause) => {
                    data.interrupt_cause = Some(cause);
                    Ok(UpdateDeadline::Interrupt)
                }
                None => Ok(UpdateDeadline::Continue(1)),
            }
        });
        store
    }
}

/// The thread holds only a weak reference, so it exits once the last engine clone drops.
fn start_epoch_ticker(engine: EngineWeak) -> std::io::Result<JoinHandle<()>> {
    std::thread::Builder::new()
        .name("plugin-epoch-ticker".to_string())
        .spawn(move || {
            while let Some(engine) = engine.upgrade() {
                engine.increment_epoch();
                drop(engine);
                std::thread::sleep(EPOCH_TICK_INTERVAL);
            }
        })
}

#[cfg(test)]
mod tests {
    use std::sync::mpsc;

    use super::*;

    #[test]
    fn ticker_thread_exits_after_the_last_engine_clone_drops() {
        // Arrange
        let mut config = wasmtime::Config::new();
        config.epoch_interruption(true);
        let engine = Engine::new(&config).unwrap();
        let ticker = start_epoch_ticker(engine.weak()).unwrap();
        let (sender, receiver) = mpsc::channel();
        std::thread::spawn(move || sender.send(ticker.join().is_ok()));

        // Act
        drop(engine);

        let exited = receiver.recv_timeout(Duration::from_secs(2));
        assert_eq!(exited, Ok(true));
    }

    #[test]
    fn a_new_store_starts_with_no_interrupt_cause() {
        // Arrange
        let runtime = PluginRuntime::new(PluginLimitsSettings::default()).unwrap();
        let deadline = Instant::now() + Duration::from_secs(60);

        // Act
        let store = runtime.new_store(deadline, CancellationToken::new());

        // Assert
        assert_eq!(store.data().interrupt_cause(), None);
        assert!(!store.data().cancel.is_cancelled());
    }
}
