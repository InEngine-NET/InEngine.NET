# Server

When run as a server, InEngine runs scheduled commands and consumes queued messages.

## Running the Server

Start the server in the foreground:

```bash
inengine -s
```

Use a custom configuration file:

```bash
inengine -c /etc/inengine/production.toml -s
```

## Stopping the Server

The server stops on `SIGINT` (Ctrl+C) or `SIGTERM`.
It stops each running plugin call within about 10 milliseconds.
A queued message whose plugin call was stopped returns to pending, and the attempt does not count.
The server waits for other running commands to finish.

## Running with systemd

Create a unit file at `/etc/systemd/system/inengine.service`:

```ini
[Unit]
Description=InEngine
After=network.target

[Service]
Type=simple
User=inengine
WorkingDirectory=/opt/inengine
ExecStart=/opt/inengine/inengine -s
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

Enable and start the service:

```bash
sudo systemctl enable inengine
sudo systemctl start inengine
```

## Running with Supervisor

Create a configuration file at `/etc/supervisor/conf.d/inengine.conf`:

```ini
[program:inengine]
process_name=%(program_name)s_%(process_num)02d
directory=/opt/inengine
command=/opt/inengine/inengine -s
autostart=true
autorestart=true
user=inengine
numprocs=1
redirect_stderr=true
stdout_logfile=/var/log/inengine.log
```

Reload and start:

```bash
sudo supervisorctl reread
sudo supervisorctl update
sudo supervisorctl start inengine:*
```

## Logging

Set the `INENGINE_LOG` environment variable to control log output:

```bash
INENGINE_LOG=info inengine -s
```
