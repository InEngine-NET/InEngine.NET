//! A plugin that the inengine-core tests load to exercise timeouts, memory limits,
//! failures, and schedule entries that the host must reject.

wit_bindgen::generate!({
    path: "../inengine-core/wit",
    world: "plugin",
});

use exports::inengine::plugin::commands::{CommandDescriptor, Guest as Commands};
use exports::inengine::plugin::scheduling::{Guest as Scheduling, ScheduleEntry};

struct FixturePlugin;

export!(FixturePlugin);

#[cfg_attr(feature = "spin-on-load", allow(dead_code))]
fn descriptor(name: &str) -> CommandDescriptor {
    CommandDescriptor {
        name: name.to_string(),
        help_text: String::new(),
    }
}

fn entry(command_name: &str, cron_expression: &str) -> ScheduleEntry {
    ScheduleEntry {
        command_name: command_name.to_string(),
        cron_expression: cron_expression.to_string(),
    }
}

fn allocate(args_json: &str) -> Result<String, String> {
    let mib = serde_json::from_str::<serde_json::Value>(args_json)
        .ok()
        .and_then(|args| {
            let mib = args.get("mib")?;
            mib.as_u64()
                .or_else(|| mib.as_str()?.parse().ok())
                .and_then(|mib| usize::try_from(mib).ok())
        })
        .ok_or("allocate needs a mib argument with a whole number")?;
    let mut buffer = vec![0u8; mib * 1024 * 1024];
    // Touch every page so the allocation cannot be optimized away.
    for byte in buffer.iter_mut().step_by(4096) {
        *byte = 1;
    }
    Ok(std::hint::black_box(buffer).len().to_string())
}

impl Commands for FixturePlugin {
    fn list_commands() -> Vec<CommandDescriptor> {
        #[cfg(feature = "spin-on-load")]
        loop {
            std::hint::black_box(());
        }
        #[cfg(not(feature = "spin-on-load"))]
        ["echo-args", "spin", "allocate", "fail"]
            .into_iter()
            .map(descriptor)
            .collect()
    }

    fn run_command(name: String, args_json: String) -> Result<String, String> {
        match name.as_str() {
            "echo-args" => Ok(args_json),
            "spin" => loop {
                std::hint::black_box(());
            },
            "allocate" => allocate(&args_json),
            "fail" => Err("fixture failure".to_string()),
            _ => Err(format!("unknown command: {name}")),
        }
    }
}

impl Scheduling for FixturePlugin {
    fn list_schedules() -> Vec<ScheduleEntry> {
        vec![
            entry("echo-args", "0 0 0 1 1 *"),
            entry("missing", "0 0 0 1 1 *"),
            entry("echo-args", "not a cron"),
        ]
    }
}
