# Plugins

InEngine loads plugins as WASM components built with the WebAssembly Component Model.
A plugin provides commands and schedules.
A plugin command can run from the command line, from the queue, or on a schedule.

## Running a Plugin Command

```bash
inengine plugin --name sample --command greet --args name=Ethan
```

## Deploying a Plugin

1. Build the plugin as a `.wasm` component.
2. Copy the `.wasm` file to a directory.
3. Add the directory to `inengine.toml`:

```toml
[plugins]
sample = "/path/to/plugin/directory"
```

The key is the plugin name.
InEngine loads every `.wasm` file in the directory.
A relative path resolves against the working directory.
The name `inengine-core` is reserved for the built-in commands.

## Queuing a Plugin Command

Publish a plugin command with the plugin name and the command name:

```bash
inengine queue publish --plugin sample --command greet --args name=Ethan
```

The server and `inengine queue consume` both load the plugins in `inengine.toml`, so either one can run the message.

## WIT Interface

Plugins implement the `inengine:plugin` WIT world.
The interface has two parts: commands and scheduling.

```wit
package inengine:plugin;

interface commands {
    record command-descriptor {
        name: string,
        help-text: string,
    }

    list-commands: func() -> list<command-descriptor>;
    run-command: func(name: string, args-json: string) -> result<string, string>;
}

interface scheduling {
    record schedule-entry {
        command-name: string,
        cron-expression: string,
    }

    list-schedules: func() -> list<schedule-entry>;
}

world plugin {
    export commands;
    export scheduling;
}
```

## Scheduling from a Plugin

Return `ScheduleEntry` records from `list_schedules` to schedule a plugin command:

```rust
fn list_schedules() -> Vec<exports::inengine::plugin::scheduling::ScheduleEntry> {
    vec![
        exports::inengine::plugin::scheduling::ScheduleEntry {
            command_name: "greet".to_string(),
            cron_expression: "0 */5 * * * *".to_string(),
        },
    ]
}
```

The server runs each entry on its cron schedule, with the arguments `{}`.
`inengine schedule list` shows each entry as a job named `<plugin>::<command>`.
An entry for a command that the plugin does not list, or with a cron expression that does not parse, is skipped with a warning.

## Limits

Each plugin call runs in a sandbox with two limits from the `[plugin_limits]` table of `inengine.toml`:

```toml
[plugin_limits]
timeout_seconds = 900
memory_mib = 256
```

A call that runs longer than `timeout_seconds` is stopped and fails.
A scheduled job with a shorter timeout stops the call at that timeout.
A call that needs more memory than `memory_mib` fails.
Loading a plugin has a fixed limit of 10 seconds.

## Writing a Plugin

### Prerequisites

Install `cargo-component` and the `wasm32-unknown-unknown` target:

```bash
cargo install cargo-component --locked
rustup target add wasm32-unknown-unknown
```

From the `rust` directory, `just install-tools` installs both.

### Create the Crate

Create a new Rust library crate with `crate-type = ["cdylib"]`.
Add `wit-bindgen` and `serde_json` as dependencies.
Copy the `rust/inengine-core/wit/plugin.wit` file from the InEngine repository into a `wit` folder in your crate.
cargo-component then writes `src/bindings.rs` on each build.
Your crate does not use that file, because `wit_bindgen::generate!` generates the bindings, so add it to `.gitignore`.

### Implement the Interfaces

Use `wit_bindgen::generate!` to generate guest bindings, then implement the traits:

```rust
wit_bindgen::generate!({
    path: "wit",
    world: "plugin",
});

struct MyPlugin;

export!(MyPlugin);

impl exports::inengine::plugin::commands::Guest for MyPlugin {
    fn list_commands() -> Vec<exports::inengine::plugin::commands::CommandDescriptor> {
        vec![
            exports::inengine::plugin::commands::CommandDescriptor {
                name: "greet".to_string(),
                help_text: "Print a greeting".to_string(),
            },
        ]
    }

    fn run_command(name: String, args_json: String) -> Result<String, String> {
        match name.as_str() {
            "greet" => {
                let who = serde_json::from_str::<serde_json::Value>(&args_json)
                    .ok()
                    .and_then(|v| v.get("name").and_then(|n| n.as_str()).map(String::from))
                    .unwrap_or_else(|| "world".to_string());
                Ok(format!("Hello, {who}!"))
            }
            _ => Err(format!("unknown command: {name}")),
        }
    }
}

impl exports::inengine::plugin::scheduling::Guest for MyPlugin {
    fn list_schedules() -> Vec<exports::inengine::plugin::scheduling::ScheduleEntry> {
        vec![]
    }
}
```

### Build

From the plugin directory:

```bash
cargo component build --release --target wasm32-unknown-unknown
```

The compiled component is written to `target/wasm32-unknown-unknown/release/<crate_name>.wasm`.

Build for `wasm32-unknown-unknown` rather than a WASI target.
The host provides no WASI imports, so a component built for `wasm32-wasip1` or `wasm32-wasip2` fails to load with an error about a missing `wasi:cli` implementation.

From the `rust` directory, `just build-sample-plugin` builds the sample plugin and copies it to `target/plugins/sample/`.

### Command Arguments

The `args_json` parameter is a JSON object.
When a user passes `--args key=value` pairs, they arrive as `{"key": "value"}`.
