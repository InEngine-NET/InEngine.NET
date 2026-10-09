# InEngine

InEngine is a plugin-based application that allows [commands](commands.md) to be [queued](queuing.md), [scheduled](scheduling.md), and run directly.

## How It Works

InEngine uses a WASM plugin system to load and run commands.
It also has a built-in command to run external programs.

Run the **echo** command:

```bash
inengine echo --text "Hello, world"
```

Run an external program with the **exec** command:

```bash
inengine exec -e ls -- -lhp
```

## Quick Start: Queuing

Publish a command to the queue:

```bash
inengine queue publish --plugin inengine-core --command echo --args "text=Hello, world"
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

Run a command from a WASM plugin:

```bash
inengine plugin --name sample-plugin --command greet --args name=Ethan
```
