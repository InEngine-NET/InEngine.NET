use std::str::FromStr;
use std::sync::Arc;

use crate::command::Command;
use crate::commands::chain::Chain;
use crate::config::Settings;
use crate::server::lifecycle::CommandLifecycle;

#[derive(Debug, thiserror::Error)]
pub enum ScheduleError {
    #[error("invalid cron expression '{expression}': {reason}")]
    InvalidCron { expression: String, reason: String },
}

pub struct JobRegistration {
    pub command: Arc<dyn Command>,
    pub command_name: String,
    pub cron_expression: String,
    pub cron_schedule: cron::Schedule,
    pub lifecycle: CommandLifecycle,
}

impl std::fmt::Debug for JobRegistration {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.debug_struct("JobRegistration")
            .field("command_name", &self.command_name)
            .field("cron_expression", &self.cron_expression)
            .finish()
    }
}

impl JobRegistration {
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

    pub fn with_timeout(mut self, seconds: u64) -> Self {
        self.lifecycle.timeout_seconds = seconds;
        self
    }

    pub fn before<F: Fn() + Send + Sync + 'static>(mut self, f: F) -> Self {
        self.lifecycle.before_action = Some(Box::new(f));
        self
    }

    pub fn after<F: Fn() + Send + Sync + 'static>(mut self, f: F) -> Self {
        self.lifecycle.after_action = Some(Box::new(f));
        self
    }
}

pub struct ScheduleBuilder {
    pub registrations: Vec<JobRegistration>,
    pub settings: Arc<Settings>,
}

impl ScheduleBuilder {
    pub fn new(settings: Arc<Settings>) -> Self {
        Self {
            registrations: Vec::new(),
            settings,
        }
    }

    pub fn command(&self, cmd: impl Command + 'static) -> Occurrence {
        let name = cmd.name().to_string();
        Occurrence {
            command: Arc::new(cmd),
            command_name: name,
        }
    }

    pub fn commands(&self, cmds: Vec<Box<dyn Command>>) -> Occurrence {
        Occurrence {
            command: Arc::new(Chain { commands: cmds }),
            command_name: "chain".to_string(),
        }
    }

    pub fn add(&mut self, registration: JobRegistration) {
        self.registrations.push(registration);
    }
}

pub struct Occurrence {
    command: Arc<dyn Command>,
    command_name: String,
}

impl Occurrence {
    pub fn cron(self, expression: &str) -> Result<JobRegistration, ScheduleError> {
        let cron_schedule =
            cron::Schedule::from_str(expression).map_err(|e| ScheduleError::InvalidCron {
                expression: expression.to_string(),
                reason: e.to_string(),
            })?;
        Ok(JobRegistration {
            command: self.command,
            command_name: self.command_name,
            cron_expression: expression.to_string(),
            cron_schedule,
            lifecycle: CommandLifecycle::default(),
        })
    }

    pub fn every_second(self) -> Result<JobRegistration, ScheduleError> {
        self.cron("* * * * * *")
    }

    pub fn every_minute(self) -> Result<JobRegistration, ScheduleError> {
        self.cron("0 * * * * *")
    }

    pub fn every_five_minutes(self) -> Result<JobRegistration, ScheduleError> {
        self.cron("0 */5 * * * *")
    }

    pub fn every_ten_minutes(self) -> Result<JobRegistration, ScheduleError> {
        self.cron("0 */10 * * * *")
    }

    pub fn every_fifteen_minutes(self) -> Result<JobRegistration, ScheduleError> {
        self.cron("0 */15 * * * *")
    }

    pub fn every_thirty_minutes(self) -> Result<JobRegistration, ScheduleError> {
        self.cron("0 */30 * * * *")
    }

    pub fn hourly(self) -> Result<JobRegistration, ScheduleError> {
        self.cron("0 0 * * * *")
    }

    pub fn hourly_at(self, minutes: u32) -> Result<JobRegistration, ScheduleError> {
        self.cron(&format!("0 {minutes} * * * *"))
    }

    pub fn daily(self) -> Result<JobRegistration, ScheduleError> {
        self.cron("0 0 0 * * *")
    }

    pub fn daily_at(self, hours: u32, minutes: u32) -> Result<JobRegistration, ScheduleError> {
        self.cron(&format!("0 {minutes} {hours} * * *"))
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::command::{CommandContext, CommandResult};
    use crate::commands::test_helpers::make_ctx;

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

    fn make_occurrence() -> Occurrence {
        Occurrence {
            command: Arc::new(TestCmd { label: "t".into() }),
            command_name: "t".into(),
        }
    }

    #[test]
    fn every_second_produces_correct_cron() {
        let reg = make_occurrence().every_second().unwrap();
        assert_eq!(reg.cron_expression, "* * * * * *");
    }

    #[test]
    fn every_minute_produces_correct_cron() {
        let reg = make_occurrence().every_minute().unwrap();
        assert_eq!(reg.cron_expression, "0 * * * * *");
    }

    #[test]
    fn every_five_minutes_produces_correct_cron() {
        let reg = make_occurrence().every_five_minutes().unwrap();
        assert_eq!(reg.cron_expression, "0 */5 * * * *");
    }

    #[test]
    fn every_ten_minutes_produces_correct_cron() {
        let reg = make_occurrence().every_ten_minutes().unwrap();
        assert_eq!(reg.cron_expression, "0 */10 * * * *");
    }

    #[test]
    fn every_fifteen_minutes_produces_correct_cron() {
        let reg = make_occurrence().every_fifteen_minutes().unwrap();
        assert_eq!(reg.cron_expression, "0 */15 * * * *");
    }

    #[test]
    fn every_thirty_minutes_produces_correct_cron() {
        let reg = make_occurrence().every_thirty_minutes().unwrap();
        assert_eq!(reg.cron_expression, "0 */30 * * * *");
    }

    #[test]
    fn hourly_produces_correct_cron() {
        let reg = make_occurrence().hourly().unwrap();
        assert_eq!(reg.cron_expression, "0 0 * * * *");
    }

    #[test]
    fn hourly_at_produces_correct_cron() {
        let reg = make_occurrence().hourly_at(27).unwrap();
        assert_eq!(reg.cron_expression, "0 27 * * * *");
    }

    #[test]
    fn daily_produces_correct_cron() {
        let reg = make_occurrence().daily().unwrap();
        assert_eq!(reg.cron_expression, "0 0 0 * * *");
    }

    #[test]
    fn daily_at_produces_correct_cron() {
        let reg = make_occurrence().daily_at(22, 30).unwrap();
        assert_eq!(reg.cron_expression, "0 30 22 * * *");
    }

    #[test]
    fn custom_cron_stores_expression() {
        let reg = make_occurrence().cron("15 * * * * *").unwrap();
        assert_eq!(reg.cron_expression, "15 * * * * *");
    }

    #[test]
    fn invalid_cron_returns_error() {
        let result = make_occurrence().cron("not a cron");
        assert!(result.is_err());
        assert!(matches!(result.unwrap_err(), ScheduleError::InvalidCron { .. }));
    }

    #[test]
    fn register_single_command() {
        // Arrange
        let ctx = make_ctx();
        let mut builder = ScheduleBuilder::new(ctx.settings.clone());

        // Act
        let reg = builder
            .command(TestCmd {
                label: "hello".into(),
            })
            .every_five_minutes()
            .unwrap();
        builder.add(reg);

        // Assert
        assert_eq!(builder.registrations.len(), 1);
        assert_eq!(builder.registrations[0].command_name, "hello");
        assert_eq!(builder.registrations[0].cron_expression, "0 */5 * * * *");
    }

    #[test]
    fn register_two_commands() {
        // Arrange
        let ctx = make_ctx();
        let mut builder = ScheduleBuilder::new(ctx.settings.clone());

        // Act
        let reg1 = builder
            .command(TestCmd {
                label: "first".into(),
            })
            .every_second()
            .unwrap();
        builder.add(reg1);

        let reg2 = builder
            .command(TestCmd {
                label: "second".into(),
            })
            .daily()
            .unwrap();
        builder.add(reg2);

        // Assert
        assert_eq!(builder.registrations.len(), 2);
        assert_eq!(builder.registrations[0].command_name, "first");
        assert_eq!(builder.registrations[1].command_name, "second");
    }

    #[test]
    fn register_chain_of_commands() {
        // Arrange
        let ctx = make_ctx();
        let mut builder = ScheduleBuilder::new(ctx.settings.clone());
        let cmds: Vec<Box<dyn Command>> = vec![
            Box::new(TestCmd {
                label: "a".into(),
            }),
            Box::new(TestCmd {
                label: "b".into(),
            }),
        ];

        // Act
        let reg = builder.commands(cmds).hourly().unwrap();
        builder.add(reg);

        // Assert
        assert_eq!(builder.registrations.len(), 1);
        assert_eq!(builder.registrations[0].command_name, "chain");
    }
}
