use std::str::FromStr;
use std::sync::Arc;
use std::time::Duration;

use crate::command::Command;
use crate::commands::chain::Chain;
use crate::server::lifecycle::CommandLifecycle;

#[derive(Debug, thiserror::Error)]
pub enum ScheduleError {
    #[error("invalid cron expression '{expression}': {reason}")]
    InvalidCron { expression: String, reason: String },
}

pub struct ScheduledJob {
    pub command: Arc<dyn Command>,
    /// The command name, `chain` for a chain, or `<plugin>::<command>` for a plugin schedule.
    pub name: String,
    pub cron_expression: String,
    pub cron_schedule: cron::Schedule,
    pub lifecycle: CommandLifecycle,
}

impl std::fmt::Debug for ScheduledJob {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.debug_struct("ScheduledJob")
            .field("name", &self.name)
            .field("cron_expression", &self.cron_expression)
            .finish()
    }
}

impl ScheduledJob {
    pub fn ping_before(mut self, url: &str) -> Self {
        self.lifecycle.ping_before_url = Some(url.to_string());
        self
    }

    pub fn ping_after(mut self, url: &str) -> Self {
        self.lifecycle.ping_after_url = Some(url.to_string());
        self
    }

    pub fn write_output_to(mut self, path: &str) -> Self {
        self.lifecycle.write_output_to = Some(path.to_string());
        self
    }

    pub fn append_output_to(mut self, path: &str) -> Self {
        self.lifecycle.append_output_to = Some(path.to_string());
        self
    }

    pub fn email_output_to(mut self, address: &str) -> Self {
        self.lifecycle.email_output_to = Some(address.to_string());
        self
    }

    pub fn timeout(mut self, timeout: Duration) -> Self {
        self.lifecycle.timeout = Some(timeout);
        self
    }

    pub fn before_run<F: Fn() + Send + Sync + 'static>(mut self, f: F) -> Self {
        self.lifecycle.before_run = Some(Box::new(f));
        self
    }

    pub fn after_run<F: Fn() + Send + Sync + 'static>(mut self, f: F) -> Self {
        self.lifecycle.after_run = Some(Box::new(f));
        self
    }
}

#[derive(Default)]
pub struct Schedule {
    jobs: Vec<ScheduledJob>,
}

impl Schedule {
    pub fn new() -> Self {
        Self::default()
    }

    pub fn command(&self, cmd: impl Command + 'static) -> UnscheduledJob {
        let name = cmd.name().to_string();
        self.command_with_name(name, Arc::new(cmd))
    }

    pub fn commands(&self, cmds: Vec<Box<dyn Command>>) -> UnscheduledJob {
        self.command_with_name("chain".to_string(), Arc::new(Chain { commands: cmds }))
    }

    pub(crate) fn command_with_name(
        &self,
        name: String,
        command: Arc<dyn Command>,
    ) -> UnscheduledJob {
        UnscheduledJob { command, name }
    }

    pub fn add(&mut self, job: ScheduledJob) {
        self.jobs.push(job);
    }

    /// The jobs in the order they were added.
    pub fn jobs(&self) -> &[ScheduledJob] {
        &self.jobs
    }

    pub(crate) fn into_jobs(self) -> Vec<ScheduledJob> {
        self.jobs
    }
}

pub struct UnscheduledJob {
    command: Arc<dyn Command>,
    name: String,
}

impl UnscheduledJob {
    pub fn cron(self, expression: &str) -> Result<ScheduledJob, ScheduleError> {
        let cron_schedule =
            cron::Schedule::from_str(expression).map_err(|e| ScheduleError::InvalidCron {
                expression: expression.to_string(),
                reason: last_line(&e.to_string()),
            })?;
        Ok(ScheduledJob {
            command: self.command,
            name: self.name,
            cron_expression: expression.to_string(),
            cron_schedule,
            lifecycle: CommandLifecycle::default(),
        })
    }

    pub fn every_second(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("* * * * * *")
    }

    pub fn every_minute(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 * * * * *")
    }

    pub fn every_five_minutes(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 */5 * * * *")
    }

    pub fn every_ten_minutes(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 */10 * * * *")
    }

    pub fn every_fifteen_minutes(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 */15 * * * *")
    }

    pub fn every_thirty_minutes(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 */30 * * * *")
    }

    pub fn hourly(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 0 * * * *")
    }

    pub fn hourly_at(self, minutes: u32) -> Result<ScheduledJob, ScheduleError> {
        self.cron(&format!("0 {minutes} * * * *"))
    }

    pub fn daily(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 0 0 * * *")
    }

    pub fn daily_at(self, hours: u32, minutes: u32) -> Result<ScheduledJob, ScheduleError> {
        self.cron(&format!("0 {minutes} {hours} * * *"))
    }
}

/// The cron crate formats a parse error as the expression, a caret under the fault, and the
/// explanation, each on its own line. Only the explanation reads well inside a one-line message.
fn last_line(message: &str) -> String {
    message.lines().last().unwrap_or(message).trim().to_string()
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::command::{CommandContext, CommandResult};

    pub(crate) struct TestCmd {
        pub label: String,
    }

    impl Command for TestCmd {
        fn name(&self) -> &str {
            &self.label
        }

        fn run(&self, ctx: &mut CommandContext) -> CommandResult {
            ctx.output.line(&self.label);
            Ok(())
        }
    }

    fn make_unscheduled_job() -> UnscheduledJob {
        UnscheduledJob {
            command: Arc::new(TestCmd { label: "t".into() }),
            name: "t".into(),
        }
    }

    #[test]
    fn every_second_produces_correct_cron() {
        let job = make_unscheduled_job().every_second().unwrap();
        assert_eq!(job.cron_expression, "* * * * * *");
    }

    #[test]
    fn every_minute_produces_correct_cron() {
        let job = make_unscheduled_job().every_minute().unwrap();
        assert_eq!(job.cron_expression, "0 * * * * *");
    }

    #[test]
    fn every_five_minutes_produces_correct_cron() {
        let job = make_unscheduled_job().every_five_minutes().unwrap();
        assert_eq!(job.cron_expression, "0 */5 * * * *");
    }

    #[test]
    fn every_ten_minutes_produces_correct_cron() {
        let job = make_unscheduled_job().every_ten_minutes().unwrap();
        assert_eq!(job.cron_expression, "0 */10 * * * *");
    }

    #[test]
    fn every_fifteen_minutes_produces_correct_cron() {
        let job = make_unscheduled_job().every_fifteen_minutes().unwrap();
        assert_eq!(job.cron_expression, "0 */15 * * * *");
    }

    #[test]
    fn every_thirty_minutes_produces_correct_cron() {
        let job = make_unscheduled_job().every_thirty_minutes().unwrap();
        assert_eq!(job.cron_expression, "0 */30 * * * *");
    }

    #[test]
    fn hourly_produces_correct_cron() {
        let job = make_unscheduled_job().hourly().unwrap();
        assert_eq!(job.cron_expression, "0 0 * * * *");
    }

    #[test]
    fn hourly_at_produces_correct_cron() {
        let job = make_unscheduled_job().hourly_at(27).unwrap();
        assert_eq!(job.cron_expression, "0 27 * * * *");
    }

    #[test]
    fn daily_produces_correct_cron() {
        let job = make_unscheduled_job().daily().unwrap();
        assert_eq!(job.cron_expression, "0 0 0 * * *");
    }

    #[test]
    fn daily_at_produces_correct_cron() {
        let job = make_unscheduled_job().daily_at(22, 30).unwrap();
        assert_eq!(job.cron_expression, "0 30 22 * * *");
    }

    #[test]
    fn custom_cron_stores_expression() {
        let job = make_unscheduled_job().cron("15 * * * * *").unwrap();
        assert_eq!(job.cron_expression, "15 * * * * *");
    }

    #[test]
    fn invalid_cron_error_is_one_line_with_the_expression_and_the_reason() {
        // Arrange
        let job = make_unscheduled_job();

        // Act
        let result = job.cron("not a cron");

        // Assert
        let message = result.expect_err("the expression is invalid").to_string();
        assert_eq!(
            message,
            "invalid cron expression 'not a cron': The 'Seconds' field does not support using names. 'not' specified."
        );
    }

    #[test]
    fn invalid_cron_returns_error() {
        let result = make_unscheduled_job().cron("not a cron");
        assert!(result.is_err());
        assert!(matches!(
            result.unwrap_err(),
            ScheduleError::InvalidCron { .. }
        ));
    }

    #[test]
    fn register_single_command() {
        // Arrange
        let mut schedule = Schedule::new();

        // Act
        let job = schedule
            .command(TestCmd {
                label: "hello".into(),
            })
            .every_five_minutes()
            .unwrap();
        schedule.add(job);

        // Assert
        assert_eq!(schedule.jobs().len(), 1);
        assert_eq!(schedule.jobs()[0].name, "hello");
        assert_eq!(schedule.jobs()[0].cron_expression, "0 */5 * * * *");
    }

    #[test]
    fn register_two_commands() {
        // Arrange
        let mut schedule = Schedule::new();

        // Act
        let job1 = schedule
            .command(TestCmd {
                label: "first".into(),
            })
            .every_second()
            .unwrap();
        schedule.add(job1);

        let job2 = schedule
            .command(TestCmd {
                label: "second".into(),
            })
            .daily()
            .unwrap();
        schedule.add(job2);

        // Assert
        assert_eq!(schedule.jobs().len(), 2);
        assert_eq!(schedule.jobs()[0].name, "first");
        assert_eq!(schedule.jobs()[1].name, "second");
    }

    #[test]
    fn register_chain_of_commands() {
        // Arrange
        let mut schedule = Schedule::new();
        let cmds: Vec<Box<dyn Command>> = vec![
            Box::new(TestCmd { label: "a".into() }),
            Box::new(TestCmd { label: "b".into() }),
        ];

        // Act
        let job = schedule.commands(cmds).hourly().unwrap();
        schedule.add(job);

        // Assert
        assert_eq!(schedule.jobs().len(), 1);
        assert_eq!(schedule.jobs()[0].name, "chain");
    }
}
