use wasmtime::ResourceLimiter;

use crate::config::BYTES_PER_MIB;

/// Each table element costs host memory, so the total across a store has a fixed bound.
pub(crate) const TABLE_ELEMENTS_LIMIT: usize = 100_000;
/// The bound for instances, memories, and tables in one store. A component made by
/// `cargo component` uses a few of each.
pub(crate) const INSTANCE_COUNT_LIMIT: usize = 32;

/// A plugin tried to grow its memory past its budget.
#[derive(Debug, thiserror::Error)]
#[error("plugin memory would grow past its limit of {} MiB", .limit_bytes / BYTES_PER_MIB)]
pub struct MemoryLimitExceeded {
    pub limit_bytes: usize,
}

/// Bounds the total memory of every linear memory in one store, rather than each memory
/// alone, so a plugin cannot multiply its budget with more memories.
pub(crate) struct PluginLimiter {
    memory_limit_bytes: usize,
    memory_bytes: usize,
    /// The bytes of the last approved growth, returned when the host then fails to grow.
    last_memory_growth: usize,
    table_elements: usize,
}

impl PluginLimiter {
    pub(crate) fn new(memory_limit_bytes: usize) -> Self {
        Self {
            memory_limit_bytes,
            memory_bytes: 0,
            last_memory_growth: 0,
            table_elements: 0,
        }
    }
}

impl ResourceLimiter for PluginLimiter {
    fn memory_growing(
        &mut self,
        current: usize,
        desired: usize,
        maximum: Option<usize>,
    ) -> wasmtime::Result<bool> {
        if maximum.is_some_and(|maximum| desired > maximum) {
            return Ok(false);
        }
        let total = self
            .memory_bytes
            .saturating_sub(current)
            .saturating_add(desired);
        if total > self.memory_limit_bytes {
            return Err(MemoryLimitExceeded {
                limit_bytes: self.memory_limit_bytes,
            }
            .into());
        }
        self.last_memory_growth = total - self.memory_bytes.min(total);
        self.memory_bytes = total;
        Ok(true)
    }

    fn memory_grow_failed(&mut self, _error: wasmtime::Error) -> wasmtime::Result<()> {
        self.memory_bytes -= self.last_memory_growth;
        self.last_memory_growth = 0;
        Ok(())
    }

    fn table_growing(
        &mut self,
        current: usize,
        desired: usize,
        maximum: Option<usize>,
    ) -> wasmtime::Result<bool> {
        if maximum.is_some_and(|maximum| desired > maximum) {
            return Ok(false);
        }
        let total = self
            .table_elements
            .saturating_sub(current)
            .saturating_add(desired);
        if total > TABLE_ELEMENTS_LIMIT {
            return Ok(false);
        }
        self.table_elements = total;
        Ok(true)
    }

    fn instances(&self) -> usize {
        INSTANCE_COUNT_LIMIT
    }

    fn tables(&self) -> usize {
        INSTANCE_COUNT_LIMIT
    }

    fn memories(&self) -> usize {
        INSTANCE_COUNT_LIMIT
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    const MIB: usize = BYTES_PER_MIB;

    #[test]
    fn growth_within_the_budget_is_allowed() {
        // Arrange
        let mut limiter = PluginLimiter::new(16 * MIB);

        // Act
        let grown = limiter.memory_growing(0, 8 * MIB, None);

        // Assert
        assert!(grown.unwrap());
    }

    #[test]
    fn growth_past_the_budget_returns_memory_limit_exceeded() {
        // Arrange
        let mut limiter = PluginLimiter::new(16 * MIB);

        // Act
        let grown = limiter.memory_growing(0, 17 * MIB, None);

        // Assert
        let error = grown.expect_err("growth past the budget fails");
        let exceeded = error
            .downcast_ref::<MemoryLimitExceeded>()
            .expect("typed error");
        assert_eq!(exceeded.limit_bytes, 16 * MIB);
    }

    #[test]
    fn budget_covers_every_memory_together() {
        // Arrange
        let mut limiter = PluginLimiter::new(16 * MIB);
        limiter.memory_growing(0, 10 * MIB, None).unwrap();

        // Act
        let second_memory = limiter.memory_growing(0, 10 * MIB, None);

        // Assert
        assert!(second_memory.is_err());
    }

    #[test]
    fn growth_of_one_memory_counts_only_the_increase() {
        // Arrange
        let mut limiter = PluginLimiter::new(16 * MIB);
        limiter.memory_growing(0, 10 * MIB, None).unwrap();

        // Act
        let grown = limiter.memory_growing(10 * MIB, 15 * MIB, None);

        // Assert
        assert!(grown.unwrap());
    }

    #[test]
    fn growth_past_a_declared_maximum_is_refused_without_an_error() {
        // Arrange
        let mut limiter = PluginLimiter::new(16 * MIB);

        // Act
        let grown = limiter.memory_growing(0, 2 * MIB, Some(MIB));

        // Assert
        assert!(!grown.unwrap());
    }

    #[test]
    fn table_growth_past_the_element_limit_is_refused() {
        // Arrange
        let mut limiter = PluginLimiter::new(16 * MIB);
        limiter
            .table_growing(0, TABLE_ELEMENTS_LIMIT, None)
            .unwrap();

        // Act
        let grown = limiter.table_growing(0, 1, None);

        // Assert
        assert!(!grown.unwrap());
    }

    #[test]
    fn a_growth_that_fails_after_approval_returns_its_bytes_to_the_budget() {
        // Arrange
        let mut limiter = PluginLimiter::new(16 * MIB);
        limiter.memory_growing(0, 10 * MIB, None).unwrap();

        // Act
        limiter
            .memory_grow_failed(wasmtime::Error::msg("the host could not grow"))
            .unwrap();

        // Assert
        assert!(limiter.memory_growing(0, 16 * MIB, None).unwrap());
    }
}
