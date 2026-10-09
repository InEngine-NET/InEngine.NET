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

Set a timeout in seconds (default is 900). Use 0 for no limit:

```bash
inengine exec -e hello -t 60
```

## Chain

Multiple commands can run in sequence as a chain.
If one command in the chain fails, the rest do not run.

## Output

Commands can write colored text to the terminal.
The available output methods are:

| Method           | Color   | Newline |
| ---------------- | ------- | ------- |
| `line`           | white   | yes     |
| `info`           | green   | yes     |
| `warning`        | yellow  | yes     |
| `error`          | red     | yes     |
| `text`           | white   | no      |
| `info_text`      | green   | no      |
| `warning_text`   | yellow  | no      |
| `error_text`     | red     | no      |

Display blank lines with `newline`.

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
