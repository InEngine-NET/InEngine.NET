use std::path::PathBuf;
use std::str::FromStr;
use std::sync::Arc;
use std::time::Duration;

use crate::command::Command;
use crate::commands::chain::Chain;
use crate::server::lifecycle::CommandLifecycle;

/// An error that occurs while you build a schedule.
#[derive(Debug, thiserror::Error)]
pub enum ScheduleError {
    /// The cron expression did not parse.
    #[error("invalid cron expression '{expression}': {reason}")]
    InvalidCron {
        /// The expression as given.
        expression: String,
        /// Why the cron parser rejected the expression.
        reason: String,
    },
}

/// A command with a cron schedule and the actions that run around it.
///
/// Get one from an [`UnscheduledJob`] method, set the actions with the builder methods, and pass it
/// to [`Schedule::add`].
pub struct ScheduledJob {
    /// The command that runs each time the schedule fires.
    pub command: Arc<dyn Command>,
    /// The command name, `chain` for a chain, or `<plugin>::<command>` for a plugin schedule.
    pub name: String,
    /// The cron expression as given, with six fields that start with seconds.
    pub cron_expression: String,
    /// The parsed form of `cron_expression`. Fire times are computed in UTC.
    pub cron_schedule: cron::Schedule,
    /// The pings, output targets, time limit, and hooks that the builder methods set.
    pub lifecycle: CommandLifecycle,
}

impl std::fmt::Debug for ScheduledJob {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.debug_struct("ScheduledJob")
            .field("name", &self.name)
            .field("cron_expression", &self.cron_expression)
            .field("lifecycle", &self.lifecycle)
            .finish()
    }
}

impl ScheduledJob {
    /// Sends a GET request to `url` before each run. When the request fails, the command does not
    /// run and the run fails.
    pub fn ping_before(mut self, url: impl Into<String>) -> Self {
        self.lifecycle.options.ping_before_url = Some(url.into());
        self
    }

    /// Sends a GET request to `url` after each successful run. A failed run sends no request.
    /// When the request fails, the run fails, and its output is not written, appended, or mailed.
    pub fn ping_after(mut self, url: impl Into<String>) -> Self {
        self.lifecycle.options.ping_after_url = Some(url.into());
        self
    }

    /// Replaces the file at `path` with the output of each run. A failed run also writes its
    /// output.
    pub fn write_output_to(mut self, path: impl Into<PathBuf>) -> Self {
        self.lifecycle.options.write_output_to = Some(path.into());
        self
    }

    /// Adds the output of each run to the end of the file at `path`, and creates the file when it
    /// is missing. A failed run also adds its output.
    pub fn append_output_to(mut self, path: impl Into<PathBuf>) -> Self {
        self.lifecycle.options.append_output_to = Some(path.into());
        self
    }

    /// Mails the output of each run to `address` through the `[mail]` server. A failed run also
    /// sends mail, with the error added to the body.
    pub fn email_output_to(mut self, address: impl Into<String>) -> Self {
        self.lifecycle.options.email_output_to = Some(address.into());
        self
    }

    /// Gives each run a deadline `timeout` after it starts. A command that stops at the deadline
    /// fails and its output is saved. When the command is still running one second later, the
    /// server stops waiting for it and saves no output. That command keeps its thread until it
    /// returns, so the next run of the job can start while it still runs.
    pub fn timeout(mut self, timeout: Duration) -> Self {
        self.lifecycle.options.timeout = Some(timeout);
        self
    }

    /// `f` runs on an async worker thread before the command, so it must not block.
    pub fn before_run<F: Fn() + Send + Sync + 'static>(mut self, f: F) -> Self {
        self.lifecycle.hooks.before_run = Some(Box::new(f));
        self
    }

    /// `f` runs on an async worker thread after the command succeeds, so it must not block.
    pub fn after_run<F: Fn() + Send + Sync + 'static>(mut self, f: F) -> Self {
        self.lifecycle.hooks.after_run = Some(Box::new(f));
        self
    }
}

/// The jobs that the scheduler runs while the server is up.
///
/// Start each job with [`Schedule::command`] or [`Schedule::commands`], then add it with
/// [`Schedule::add`].
#[derive(Default)]
pub struct Schedule {
    jobs: Vec<ScheduledJob>,
}

impl Schedule {
    /// Creates a schedule with no jobs.
    pub fn new() -> Self {
        Self::default()
    }

    /// Starts a job that runs `cmd`. Call a method on the result to set when it runs.
    pub fn command(&self, cmd: impl Command + 'static) -> UnscheduledJob {
        let name = cmd.name().to_string();
        self.command_with_name(name, Arc::new(cmd))
    }

    /// Starts a job that runs `cmds` in order as one chain. The chain stops at the first command
    /// that fails.
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

    /// Adds `job` to the schedule. The scheduler runs it when the server starts.
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

/// A command that does not have a schedule yet. Call one of its methods to get a [`ScheduledJob`].
pub struct UnscheduledJob {
    command: Arc<dyn Command>,
    name: String,
}

impl UnscheduledJob {
    /// Runs the job on a cron expression with six fields: second, minute, hour, day of month,
    /// month, and day of week. Fire times are in UTC.
    ///
    /// Returns an error when the expression does not parse.
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

    /// Runs the job at the start of every second.
    pub fn every_second(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("* * * * * *")
    }

    /// Runs the job at second 0 of every minute.
    pub fn every_minute(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 * * * * *")
    }

    /// Runs the job at minutes 0, 5, 10, and so on, of every hour.
    pub fn every_five_minutes(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 */5 * * * *")
    }

    /// Runs the job at minutes 0, 10, 20, and so on, of every hour.
    pub fn every_ten_minutes(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 */10 * * * *")
    }

    /// Runs the job at minutes 0, 15, 30, and 45 of every hour.
    pub fn every_fifteen_minutes(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 */15 * * * *")
    }

    /// Runs the job at minutes 0 and 30 of every hour.
    pub fn every_thirty_minutes(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 */30 * * * *")
    }

    /// Runs the job at minute 0 of every hour.
    pub fn hourly(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 0 * * * *")
    }

    /// Runs the job at `minutes` past every hour, in UTC. A value above 59 returns an error.
    pub fn hourly_at(self, minutes: u32) -> Result<ScheduledJob, ScheduleError> {
        self.cron(&format!("0 {minutes} * * * *"))
    }

    /// Runs the job at midnight UTC every day.
    pub fn daily(self) -> Result<ScheduledJob, ScheduleError> {
        self.cron("0 0 0 * * *")
    }

    /// Runs the job every day at `hours`:`minutes` UTC. An hour above 23 or a minute above 59
    /// returns an error.
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
