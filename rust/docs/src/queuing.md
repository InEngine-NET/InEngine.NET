# Queuing

InEngine's queue allows commands to run in the background.
A command should be queued when it needs to run once, as soon as possible, outside the current process.

## Queue Drivers

Set the `queue_driver` field in [inengine.toml](configuration.md) to select a driver.

### File

The file driver writes queued messages to the file system.
It is useful for development and testing.

```toml
[queue]
queue_driver = "file"

[queue.file]
base_path = "../"
```

### Redis

Redis is suitable for production use.
InEngine uses Redis `LMOVE` for durable queue operations.
Messages are not lost if the process stops.

Redis 6.2 or later is required.

```toml
[queue]
queue_driver = "redis"

[queue.redis]
host = "127.0.0.1"
port = 6379
database = 0
password = ""
```

## Publishing Commands

Publish a command to the queue from the command line:

```bash
inengine queue publish --plugin inengine-core --command echo --args "text=Hello"
```

Publish by class name:

```bash
inengine queue publish --plugin inengine-core --class "inengine_core::commands::echo::Echo" --args "text=Hello"
```

Publish with a JSON payload:

```bash
inengine queue publish --plugin inengine-core --command echo --json '{"text":"Hello"}'
```

Publish to the secondary queue:

```bash
inengine queue publish --plugin inengine-core --command echo --args "text=Hello" --secondary
```

Set the maximum number of retries (0 to 100):

```bash
inengine queue publish --plugin inengine-core --command echo --args "text=Hello" --max-retries 3
```

## Consuming Commands

Consume one message:

```bash
inengine queue consume
```

Consume a specific number of messages:

```bash
inengine queue consume --count 10
```

Consume all messages:

```bash
inengine queue consume --all
```

Consume from the secondary queue:

```bash
inengine queue consume --secondary
```

## Viewing Queue Lengths

The `queue length` command shows a count of pending, in-progress, and failed messages:

```bash
inengine queue length
```

## Viewing Queue Status

```bash
inengine queue status
```

## Peeking at Queued Commands

View pending messages:

```bash
inengine queue peek --pending
```

View failed messages:

```bash
inengine queue peek --failed
```

View in-progress messages:

```bash
inengine queue peek --in-progress
```

View messages as JSON:

```bash
inengine queue peek --pending --json
```

View a specific range (default is 0 to 9):

```bash
inengine queue peek --pending --from 0 --to 99
```

Peek at the secondary queue:

```bash
inengine queue peek --pending --secondary
```

## Republishing Failed Commands

Commands that fail are moved to a failed stage.
Republish them back to the pending queue:

```bash
inengine queue republish
```

Set a limit (default is 100):

```bash
inengine queue republish --limit 1000
```

Republish from the secondary queue:

```bash
inengine queue republish --secondary
```

## Recovering In-Progress Messages

Move in-progress messages back to the pending queue:

```bash
inengine queue recover
```

> **Warning:** Stop every other consumer of the queue first.
> Recovery cannot tell a message left by a crash from one that another consumer is still processing.
> That message would run twice.

## Flushing the Queue

Clear pending messages:

```bash
inengine queue flush --pending
```

Clear failed messages:

```bash
inengine queue flush --failed
```

Clear in-progress messages:

```bash
inengine queue flush --in-progress
```

Clear messages from the secondary queue:

```bash
inengine queue flush --pending --secondary
```

## Primary and Secondary Queue

There is no difference between the primary and secondary queues except that the primary queue is the default.
Use the secondary queue for a different class of work.
For example, send long-running jobs to the secondary queue and short jobs to the primary queue.

For more than two queues, run separate InEngine instances with different configuration files:

```bash
inengine -c queue-a.toml -s
inengine -c queue-b.toml -s
```

## Message Compression

Enable `use_compression` to compress queued messages with gzip.
This reduces memory use when commands carry a lot of internal state.
In a high-throughput scenario with limited bandwidth, compression also reduces network use.

Do not enable compression when queued commands are small.
The CPU cost of compression can reduce throughput without a benefit.

```toml
[queue]
use_compression = true
```
