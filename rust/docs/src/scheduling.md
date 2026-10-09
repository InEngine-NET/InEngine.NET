# Scheduling

[Commands](commands.md) can run on recurring cron schedules.

A scheduled command is different from a queued command.
Queue a command to run it once, as soon as possible.
Schedule a command to run it many times, on a fixed interval.

InEngine defines schedules in code, not in a data store.
This lets the schedule live alongside the command code in version control.

## Scheduling a Command

Use the `Schedule` struct to register commands with cron expressions.

```rust
use inengine_core::scheduling::schedule::Schedule;

let mut schedule = Schedule::new();

let job = schedule
    .command(MyCommand { /* ... */ })
    .every_five_minutes()?;
schedule.add(job);
```

Use a custom cron expression:

```rust
let job = schedule
    .command(MyCommand { /* ... */ })
    .cron("15 * * * * *")?;
schedule.add(job);
```

## Convenience Methods

| Method                 | Cron Expression   |
| ---------------------- | ----------------- |
| `every_second`         | `* * * * * *`     |
| `every_minute`         | `0 * * * * *`     |
| `every_five_minutes`   | `0 */5 * * * *`   |
| `every_ten_minutes`    | `0 */10 * * * *`  |
| `every_fifteen_minutes`| `0 */15 * * * *`  |
| `every_thirty_minutes` | `0 */30 * * * *`  |
| `hourly`               | `0 0 * * * *`     |
| `hourly_at(27)`        | `0 27 * * * *`    |
| `daily`                | `0 0 0 * * *`     |
| `daily_at(22, 30)`     | `0 30 22 * * *`   |

## Scheduling a Chain

Schedule a group of commands to run as a chain.
The chain stops if one command fails.

```rust
let cmds: Vec<Box<dyn Command>> = vec![
    Box::new(TransferFile { path: path1 }),
    Box::new(TransferFile { path: path2 }),
    Box::new(TransferFile { path: path3 }),
];

let job = schedule.commands(cmds).daily()?;
schedule.add(job);
```

## Lifecycle Methods

Scheduled jobs support lifecycle hooks.

Ping a URL before or after the command runs:

```rust
let job = schedule
    .command(MyCommand { /* ... */ })
    .every_five_minutes()?
    .ping_before("https://example.com/start")
    .ping_after("https://example.com/done");
schedule.add(job);
```

Write command output to a file:

```rust
let job = schedule
    .command(MyCommand { /* ... */ })
    .hourly()?
    .write_output_to("/var/log/my-command.log");
schedule.add(job);
```

Append command output to a file:

```rust
let job = schedule
    .command(MyCommand { /* ... */ })
    .hourly()?
    .append_output_to("/var/log/my-command.log");
schedule.add(job);
```

Email command output:

```rust
let job = schedule
    .command(MyCommand { /* ... */ })
    .daily()?
    .email_output_to("ops@example.com");
schedule.add(job);
```

Set a timeout:

```rust
use std::time::Duration;

let job = schedule
    .command(MyCommand { /* ... */ })
    .hourly()?
    .timeout(Duration::from_secs(300));
schedule.add(job);
```

Run custom logic before or after:

```rust
let job = schedule
    .command(MyCommand { /* ... */ })
    .every_five_minutes()?
    .before_run(|| println!("starting"))
    .after_run(|| println!("finished"));
schedule.add(job);
```

## Running a Schedule

Give the schedule to the server:

```rust
use std::sync::Arc;

use inengine_core::queue::CommandRegistry;
use inengine_core::server::host::Server;

let server = Server::new(settings, Arc::new(CommandRegistry::new()), schedule);
server.run(cancel).await;
```

A plugin can also add schedules.
See [Plugins](plugins.md#scheduling-from-a-plugin).

## Viewing Scheduled Commands

List the plugin schedules and their next fire time:

```bash
inengine schedule list
```

## Running the Scheduler

The [server](server.md) runs the plugin schedules:

```bash
inengine -s
```

A schedule built in Rust code runs only in a server that your program starts with `Server::new`.
