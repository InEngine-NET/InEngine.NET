# Commands

Commands can be WASM plugin modules, Rust types, or external programs.
They can be queued, scheduled, or run from the command line.

## Echo

Print text to the terminal:

```bash
inengine echo --text "Hello, world"
```

## Exec

Run an external program.
The program must be listed in the `exec_whitelist` section of `inengine.toml`.

Add a program to the whitelist:

```toml
[exec_whitelist]
hello = "/path/to/hello.py"
```

Run the program:

```bash
inengine exec -e hello
```

Pass arguments after `--`:

```bash
inengine exec -e hello -- --verbose --count 5
```

Set a timeout in seconds.
The default is 900 seconds.
Use 0 for no limit:

```bash
inengine exec -e hello -t 60
```

The program also stops at the timeout of a scheduled job and when the server stops.
Exec kills only the program it started, so a process that the program starts in the background keeps running.

## Chain

Multiple commands can run in sequence as a chain.
If one command in the chain fails, the rest do not run.
A chain is built in Rust code.
See [Scheduling a Chain](scheduling.md#scheduling-a-chain) for an example.

## Output

Commands can write colored text to the terminal.
The available output methods are:

| Method           | Color   | Newline |
| ---------------- | ------- | ------- |
| `line`           | white   | yes     |
| `info_line`      | green   | yes     |
| `warning_line`   | yellow  | yes     |
| `error_line`     | red     | yes     |
| `text`           | white   | no      |
| `info`           | green   | no      |
| `warning`        | yellow  | no      |
| `error`          | red     | no      |

Write line breaks with `newlines`, which takes the number of newline characters to write.

## Progress Bar

Commands can show a progress bar in the terminal:

1. Call `set_progress_max` with the total number of steps.
2. Call `update_progress` with the current step number.

## View Commands

Run `inengine` with no arguments to see the list of commands:

```bash
inengine
```

## View Help

Run a command with `--help` to see its options:

```bash
inengine echo --help
inengine queue publish --help
```
