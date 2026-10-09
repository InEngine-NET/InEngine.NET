mod common;

use std::sync::mpsc;
use std::sync::Arc;
use std::time::{Duration, Instant};

use common::{
    expected_lengths, fixture_directory, load_fixture_plugin, plugin_directories,
    settings_with_queue_base,
};
use inengine_core::command::{Command, CommandContext, CommandDeadline};
use inengine_core::config::PluginLimitsSettings;
use inengine_core::error::{CommandError, PluginError};
use inengine_core::output::Output;
use inengine_core::plugin::{PluginLoader, PluginRuntime};
use inengine_core::queue::{
    CommandEnvelope, CommandRegistry, PeekedMessage, Queue, QueueLane, Stage,
};
use inengine_core::scheduling::schedule::Schedule;
use inengine_core::server::host::Server;
use tokio_util::sync::CancellationToken;

const MIB: usize = 1024 * 1024;
/// Bounds each call that may spin, so a regression fails rather than hangs. It is far above
/// the limits under test, so a busy machine does not fail a correct call.
const CALL_WAIT_LIMIT: Duration = Duration::from_secs(10);

fn limits(timeout: Duration, memory_mib: usize) -> PluginLimitsSettings {
    PluginLimitsSettings {
        timeout,
        memory_bytes: memory_mib * MIB,
    }
}

fn fixture_registry(loader: &PluginLoader) -> CommandRegistry {
    let mut registry = CommandRegistry::new();
    let errors = loader.register_commands(&mut registry);
    assert!(errors.is_empty(), "{errors:?}");
    registry
}

fn fixture_command(loader: &PluginLoader, command: &str, args_json: &str) -> Arc<dyn Command> {
    fixture_registry(loader)
        .build("fixture", command, args_json)
        .expect("the fixture lists the command")
        .into()
}

struct CallResult {
    result: Result<(), CommandError>,
    output: String,
}

/// How a guarded call sets up its context. The deadline starts inside the call thread, so a
/// late thread start does not use up the timeout.
#[derive(Default)]
struct CallSetup {
    timeout: Option<Duration>,
    cancel: Option<CancellationToken>,
}

/// Runs the command on its own thread and waits at most `CALL_WAIT_LIMIT`, because a tokio
/// runtime that drops waits for a spinning blocking task forever.
fn run_guarded(command: Arc<dyn Command>, setup: CallSetup) -> Option<CallResult> {
    let (sender, receiver) = mpsc::channel();
    std::thread::spawn(move || {
        let settings = common::default_settings();
        let mut ctx = CommandContext::new(Output::new(false), Arc::new(settings));
        if let Some(timeout) = setup.timeout {
            ctx = ctx.with_deadline(CommandDeadline::after(timeout));
        }
        if let Some(cancel) = setup.cancel {
            ctx = ctx.with_cancel(cancel);
        }
        let result = command.run(&mut ctx);
        let _ = sender.send(CallResult {
            result,
            output: ctx.output.take_buffer(),
        });
    });
    receiver.recv_timeout(CALL_WAIT_LIMIT).ok()
}

fn failure_message(call: Option<CallResult>) -> String {
    let call = call.expect("the call ends within the wait limit");
    call.result.expect_err("the call fails").to_string()
}

#[test]
fn spinning_plugin_command_stops_at_the_plugin_time_limit() {
    // Arrange
    let loader = load_fixture_plugin(limits(Duration::from_secs(1), 64));
    let command = fixture_command(&loader, "spin", "{}");

    // Act
    let call = run_guarded(command, CallSetup::default());

    // Assert
    assert_eq!(
        failure_message(call),
        "command failed: command 'spin' in plugin 'fixture' exceeded the plugin time limit of 1 seconds"
    );
}

#[test]
fn spinning_command_stops_at_an_earlier_context_deadline() {
    // Arrange
    let loader = load_fixture_plugin(limits(Duration::from_secs(60), 64));
    let command = fixture_command(&loader, "spin", "{}");
    let setup = CallSetup {
        timeout: Some(Duration::from_secs(1)),
        ..CallSetup::default()
    };

    // Act
    let call = run_guarded(command, setup);

    // Assert
    let call = call.expect("the call ends within the wait limit");
    assert!(matches!(
        call.result,
        Err(CommandError::Timeout { seconds: 1 })
    ));
}

#[test]
fn cancelling_the_context_stops_a_running_command() {
    // Arrange
    let loader = load_fixture_plugin(limits(Duration::from_secs(60), 64));
    let command = fixture_command(&loader, "spin", "{}");
    let cancel = CancellationToken::new();
    let canceller = cancel.clone();
    std::thread::spawn(move || {
        std::thread::sleep(Duration::from_millis(500));
        canceller.cancel();
    });
    let setup = CallSetup {
        cancel: Some(cancel),
        ..CallSetup::default()
    };

    // Act
    let call = run_guarded(command, setup);

    // Assert
    let call = call.expect("the call ends within the wait limit");
    assert!(matches!(call.result, Err(CommandError::Interrupted)));
}

#[test]
fn a_cancelled_call_does_not_affect_a_later_call_on_the_same_runtime() {
    // Arrange
    let loader = load_fixture_plugin(PluginLimitsSettings::default());
    let cancel = CancellationToken::new();
    cancel.cancel();
    let cancelled = CallSetup {
        cancel: Some(cancel),
        ..CallSetup::default()
    };
    let first = run_guarded(fixture_command(&loader, "spin", "{}"), cancelled);
    let command = fixture_command(&loader, "echo-args", "{}");

    // Act
    let later = run_guarded(command, CallSetup::default());

    // Assert
    let first = first.expect("the first call ends");
    assert!(matches!(first.result, Err(CommandError::Interrupted)));
    let later = later.expect("the later call ends");
    assert!(later.result.is_ok(), "{:?}", later.result);
}

#[test]
fn plugin_command_resolves_through_its_plugin_name() {
    // Arrange
    let loader = load_fixture_plugin(PluginLimitsSettings::default());
    let command = fixture_command(&loader, "echo-args", r#"{"name":"Ethan"}"#);

    // Act
    let call = run_guarded(command, CallSetup::default());

    // Assert
    let call = call.expect("the call ends within the wait limit");
    assert!(call.result.is_ok(), "{:?}", call.result);
    assert_eq!(call.output, "{\"name\":\"Ethan\"}\n");
}

#[test]
fn allocation_past_the_memory_budget_fails() {
    // Arrange
    let loader = load_fixture_plugin(limits(Duration::from_secs(60), 16));
    let command = fixture_command(&loader, "allocate", r#"{"mib":64}"#);

    // Act
    let call = run_guarded(command, CallSetup::default());

    // Assert
    assert_eq!(
        failure_message(call),
        "command failed: command 'allocate' in plugin 'fixture' exceeded the memory limit of 16 MiB"
    );
}

#[test]
fn allocation_within_the_memory_budget_succeeds() {
    // Arrange
    let loader = load_fixture_plugin(limits(Duration::from_secs(60), 16));
    let command = fixture_command(&loader, "allocate", r#"{"mib":4}"#);

    // Act
    let call = run_guarded(command, CallSetup::default());

    // Assert
    let call = call.expect("the call ends within the wait limit");
    assert!(call.result.is_ok(), "{:?}", call.result);
    assert_eq!(call.output, format!("{}\n", 4 * MIB));
}

#[test]
fn guest_error_returns_a_failure_with_its_message() {
    // Arrange
    let loader = load_fixture_plugin(PluginLimitsSettings::default());
    let command = fixture_command(&loader, "fail", "{}");

    // Act
    let call = run_guarded(command, CallSetup::default());

    // Assert
    assert_eq!(
        failure_message(call),
        "command failed: command 'fail' in plugin 'fixture' failed: fixture failure"
    );
}

#[test]
fn register_schedules_adds_valid_entries_and_reports_invalid_ones() {
    // Arrange
    let loader = load_fixture_plugin(PluginLimitsSettings::default());
    let mut schedule = Schedule::new();

    // Act
    let errors = loader.register_schedules(&mut schedule);

    // Assert
    let names: Vec<&str> = schedule
        .jobs()
        .iter()
        .map(|job| job.name.as_str())
        .collect();
    assert_eq!(names, ["fixture::echo-args"]);
    assert_eq!(schedule.jobs()[0].cron_expression, "0 0 0 1 1 *");
    let messages: Vec<String> = errors.iter().map(ToString::to_string).collect();
    assert_eq!(
        messages,
        [
            "invalid schedule entry for command 'missing' in plugin 'fixture': the plugin does not list this command",
            "invalid schedule entry for command 'echo-args' in plugin 'fixture': invalid cron expression 'not a cron': The 'Seconds' field does not support using names. 'not' specified.",
        ]
    );
}

#[test]
fn plugin_schedule_job_runs_its_command_with_empty_arguments() {
    // Arrange
    let loader = load_fixture_plugin(PluginLimitsSettings::default());
    let mut schedule = Schedule::new();
    let _ = loader.register_schedules(&mut schedule);
    let command = schedule.jobs()[0].command.clone();

    // Act
    let call = run_guarded(command, CallSetup::default());

    // Assert
    let call = call.expect("the call ends within the wait limit");
    assert!(call.result.is_ok(), "{:?}", call.result);
    assert_eq!(call.output, "{}\n");
}

#[test]
fn duplicate_command_across_two_components_is_reported_once_each() {
    // Arrange
    let directory = tempfile::tempdir().unwrap();
    let fixture = fixture_directory("plugin").join("fixture_plugin.wasm");
    std::fs::copy(&fixture, directory.path().join("a.wasm")).unwrap();
    std::fs::copy(&fixture, directory.path().join("b.wasm")).unwrap();
    let runtime = PluginRuntime::new(PluginLimitsSettings::default()).unwrap();
    let (loader, load_errors) =
        PluginLoader::load(&plugin_directories("fixture", directory.path()), runtime);
    let mut registry = CommandRegistry::new();

    // Act
    let errors = loader.register_commands(&mut registry);

    // Assert
    assert!(load_errors.is_empty(), "{load_errors:?}");
    let duplicates: Vec<String> = errors
        .iter()
        .map(|error| match error {
            PluginError::DuplicateCommand(duplicate) => duplicate.command.clone(),
            other => panic!("expected DuplicateCommand, got {other:?}"),
        })
        .collect();
    assert_eq!(duplicates, ["echo-args", "spin", "allocate", "fail"]);
}

#[test]
fn component_that_spins_while_loading_fails_at_the_load_time_limit() {
    // Arrange
    let runtime = PluginRuntime::new(PluginLimitsSettings::default()).unwrap();
    let directories = plugin_directories("fixture", &fixture_directory("spin-on-load"));
    let started = Instant::now();

    // Act
    let (loader, errors) = PluginLoader::load(&directories, runtime);

    // Assert
    assert!(started.elapsed() < Duration::from_secs(15));
    assert!(loader.plugins().is_empty());
    let messages: Vec<String> = errors.iter().map(ToString::to_string).collect();
    assert_eq!(messages.len(), 1, "{messages:?}");
    assert!(messages[0].starts_with("failed to load plugin 'fixture': "));
    assert!(messages[0]
        .ends_with("fixture_plugin_spin_on_load.wasm: the plugin did not load within 10 seconds"));
}

#[tokio::test(flavor = "multi_thread", worker_threads = 4)]
async fn stopping_the_server_returns_a_running_plugin_message_to_pending() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let mut settings = settings_with_queue_base(dir.path());
    settings.queue.primary_queue_consumers = 1;
    settings.queue.secondary_queue_consumers = 0;
    let loader = load_fixture_plugin(limits(Duration::from_secs(60), 64));
    let registry = Arc::new(fixture_registry(&loader));
    let queue = Queue::open(QueueLane::Primary, &settings.queue)
        .await
        .expect("open");
    let envelope = CommandEnvelope::new("spin", "fixture", "{}", false, 0).expect("envelope");
    queue.publish(&envelope).await.expect("publish");
    let server = Server::new(Arc::new(settings), registry, Schedule::new());
    let cancel = CancellationToken::new();
    let running = tokio::spawn(server.run(cancel.clone()));
    tokio::time::sleep(Duration::from_millis(1500)).await;

    // Act
    cancel.cancel();

    // Assert
    tokio::time::timeout(CALL_WAIT_LIMIT, running)
        .await
        .expect("the server stops within the wait limit")
        .expect("the server task ends cleanly");
    assert_eq!(
        queue.lengths().await.expect("lengths"),
        expected_lengths(1, 0, 0)
    );
    let pending = queue.peek(Stage::Pending, 0..=0).await.expect("peek");
    match &pending[..] {
        [PeekedMessage::Envelope(message)] => {
            assert_eq!(message.id, envelope.id);
            assert_eq!(message.attempts, 0);
        }
        other => panic!("expected the spin message, got {other:?}"),
    }
}
