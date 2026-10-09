# InEngine

InEngine is a plugin-based application that allows [commands](commands.md) to be [queued](queuing.md), [scheduled](scheduling.md), and run directly.

## How It Works

InEngine uses a WASM plugin system to load and run commands.
It also has a built-in command to run external programs.

Run the **echo** command:

```bash
inengine echo --text "Hello, world"
```

Run an external program with the **exec** command.
The program must be listed in the `[exec_whitelist]` table of `inengine.toml`:

```toml
[exec_whitelist]
ls = "/bin/ls"
```

```bash
inengine exec -e ls -- -lhp
```

See [Exec](commands.md#exec) for the timeout and argument options.

## Quick Start: Queuing

Publish a command to the queue:

```bash
inengine queue publish --plugin inengine-core --class "inengine_core::commands::echo::Echo" --args "text=Hello, world"
```

Consume the next message from the queue:

```bash
inengine queue consume
```

## Quick Start: Server Mode

Start the server to run scheduled commands and consume queued messages:

```bash
inengine -s
```

## Quick Start: Plugins

Add the plugin directory to the `[plugins]` table of `inengine.toml`:

```toml
[plugins]
sample = "target/plugins/sample"
```

Run a command from the plugin:

```bash
inengine plugin --name sample --command greet --args name=Ethan
```
