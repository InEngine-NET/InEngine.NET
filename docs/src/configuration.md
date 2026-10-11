# Configuration

## Overview

Configuration is stored in an `inengine.toml` file.
Use the `-c` flag to specify a different file:

```bash
inengine -c /etc/inengine/production.toml echo --text "hello"
```

Copy `inengine.example.toml` to get started.
Every setting has a default value.
Keep only the lines you change.

## Full Example

```toml
[plugins]
sample = "plugins/sample"

[exec_whitelist]
hello = "/usr/bin/hello"

[mail]
host = "localhost"
port = 25
from = "no-reply@example.com"
username = ""
password = ""
encryption = "none"

[queue]
use_compression = false
primary_queue_consumers = 8
secondary_queue_consumers = 8
queue_driver = "file"
queue_name = "InEngineQueue"
max_retries = 0
recover_on_start = false

[queue.redis]
host = "127.0.0.1"
port = 6379
database = 0
password = ""

[queue.file]
base_path = "../"

[plugin_limits]
timeout_seconds = 900
memory_mib = 256
```

## Plugins

| Setting   | Type   | Description                                        |
| --------- | ------ | -------------------------------------------------- |
| *key*     | string | The plugin name. `inengine-core` is reserved.      |
| *value*   | string | The directory that contains the `.wasm` file.      |

## Exec Whitelist

| Setting   | Type   | Description                                        |
| --------- | ------ | -------------------------------------------------- |
| *key*     | string | The name a caller passes to the exec command.      |
| *value*   | string | The file system path of the executable.            |

## Mail

| Setting    | Type    | Default       | Description                        |
| ---------- | ------- | ------------- | ---------------------------------- |
| host       | string  | `localhost`   | The SMTP server hostname.          |
| port       | integer | `25`          | The SMTP server port (1 to 65535). |
| from       | string  | `""`          | The sender email address.          |
| username   | string  | `""`          | The SMTP user name.                |
| password   | string  | `""`          | The SMTP password.                 |
| encryption | string  | `none`        | `none`, `starttls`, or `tls`.      |

`none` sends mail in plain text.
`starttls`, usually on port 587, upgrades the connection to TLS and refuses a server that cannot.
`tls`, usually on port 465, uses TLS from the first byte.
A user name with `none` is an error unless the host is `localhost` or a loopback address, so a password never crosses the network in plain text.
A port that does not match the encryption is a warning.

## Queue

### General

| Setting                    | Type    | Default            | Description                                                  |
| -------------------------- | ------- | ------------------ | ------------------------------------------------------------ |
| use_compression            | bool    | `false`            | Compress queued messages with gzip.                           |
| primary_queue_consumers    | integer | `8`                | The number of consumer tasks for the primary queue.           |
| secondary_queue_consumers  | integer | `8`                | The number of consumer tasks for the secondary queue.         |
| queue_driver               | string  | `"file"`           | The queue backend: `"file"` or `"redis"`.                    |
| queue_name                 | string  | `"InEngineQueue"`  | The base name for queue keys and directories.                 |
| max_retries                | integer | `0`                | How many times a failed message is retried before it moves to the failed stage. |
| recover_on_start           | bool    | `false`            | Move in-progress messages back to pending when the server starts. |

> **Warning:** Enable `recover_on_start` only when this server is the only consumer of the queue.
> Recovery cannot tell a message left by a crash from one that another consumer is still processing.

### Redis

Used when `queue_driver` is `"redis"`.
Requires Redis 6.2 or later.

| Setting    | Type    | Default       | Description                         |
| ---------- | ------- | ------------- | ----------------------------------- |
| host       | string  | `"127.0.0.1"` | The Redis hostname.                |
| port       | integer | `6379`        | The Redis port (1 to 65535).        |
| database   | integer | `0`           | The Redis database (0 to 255).      |
| password   | string  | `""`          | The Redis auth password.            |

### File

Used when `queue_driver` is `"file"`.

| Setting    | Type   | Default  | Description                                          |
| ---------- | ------ | -------- | ---------------------------------------------------- |
| base_path  | string | `"../"` | The directory where queue subdirectories are created. |

## Plugin Limits

| Setting         | Type    | Default | Description                                                              |
| --------------- | ------- | ------- | ------------------------------------------------------------------------ |
| timeout_seconds | integer | `900`   | The longest time one plugin call may run, in seconds (1 to 86400).       |
| memory_mib      | integer | `256`   | The memory budget of one plugin instance, in MiB (16 to 4096).           |

## Logging

InEngine uses the `INENGINE_LOG` environment variable to control log output.
The variable accepts any valid `tracing` filter directive.

```bash
INENGINE_LOG=info inengine -s
```

```bash
INENGINE_LOG=inengine_core=debug inengine -s
```

The default level is `warn`.
