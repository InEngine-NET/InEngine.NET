# Scheduling

[Commands](commands) can be scheduled to run by leveraging the inengine.exe program, available as a download from a recent [release](https://github.com/InEngine-NET/InEngine.NET/releases).

## Scheduling a Command

A job schedule is created by adding a class to your plugin assembly that implements the **InEngine.Core.IJobs** interface.

```c#

using System;
using Quartz;

namespace MyCommandPlugin
{
    public class Jobs : IJobs
    {
        public void Schedule(IScheduler scheduler)
        {
            // Schedule some jobs
        }
    }
}
```

This class is automatically discovered by the InEngine.NET scheduler.
It will call the Jobs.Schedule method with an initialized **InEngine.Scheduling.Schedule** object.

```c#
using System;
using Quartz;

namespace MyCommandPlugin
{
    public class Jobs : IJobs
    {
        public void Schedule(Schedule schedule)
        {
            schedule.Job(new MyCommand()).EveryFiveMinutes();
        }
    }
}
```

Run a command on a custom cron schedule (in this example every 15 seconds):

```c#
schedule.Job(new MyCommand()).Cron("15 * * * * ?");
```

Run a command every second:

```c#
schedule.Job(new MyCommand()).EverySecond();
```


Run a command every minute:

```c#
schedule.Job(new MyCommand()).EveryMinute();
```

Run a command every 5 minutes:

```c#
schedule.Job(new MyCommand()).EveryFiveMinutes();
```

Run a command every 10 minutes:

```c#
schedule.Job(new MyCommand()).EveryTenMinutes();
```

Run a command every 15 minutes:

```c#
schedule.Job(new MyCommand()).EveryFifteenMinutes();
```

Run a command every 30 minutes:

```c#
schedule.Job(new MyCommand()).EveryThirtyMinutes();
```

Run a command hourly:

```c#
schedule.Job(new MyCommand()).Hourly();
```


Run a command hourly at a certain number of minutes past the hour (27 minutes in this example):

```c#
schedule.Job(new MyCommand()).HourlyAt(27);
```

Run a command daily:

```c#
schedule.Job(new MyCommand()).Daily();
```

Run a command daily at a specific time (at 10:30pm in this example):

```c#
schedule.Job(new MyCommand()).DailyAt(22, 30);
```

## Command State

Commands can have properties like any C# class.
When running from the command line these properties are usually initialized with command line arguments.
When run by the scheduler, the properties are specified when the command is scheduled.
For example, this command's **Foo** property will be auto-wired to "bar" when the command is later executed by the scheduler. 

```c#
schedule
    .Job(new MyCommand() {
        Foo = "bar"
    })
    .EveryFiveMinutes();
```

If it is not desirable to auto-wire a property for some reason, simply decorate the property in the command class with the **InEngine.Core.Scheduling.DoNotAutoWireAttribute** class. 

```c#
using System;
using InEngine.Core;

namespace MyCommandPlugin
{
    public class MyCommand : AbstractCommand
    {
        [DoNotAutoWire]
        public string Foo { get; set; }
        
        public override void Run()
        {
            // Foo will be null here even if it is initialized before being scheduled. 
        }
    }
}
```

## Running the Scheduler

### Manually from the CLI

Running the scheduler from the CommandLine is useful for debugging or local development:

```bash
inengine.exe -s
```

It can also be run on Mac and Linux with Mono via a shell wrapper script:

```bash
./inengine -s
``` 

### On Windows as a Service

#### Installing
Run the Install.ps1 PowerShell script in the scheduler directory to install the scheduler in place. 
The script needs to be run as an administrator. 
The script will register the service at the location where the script is run.

```bash
ps Install.ps1
```

#### Uninstalling

Simply run the **Uninstall.ps1** script with elevated permissions to remove the service.

```bash
ps Uninstall.ps1
```

### On Linux with Supervisor

Supervisor is a process control system for Linux. 
It has extensive [documentation](http://supervisord.org/index.html), but the following should be enough to get started.

#### Installing Supervisor

This command installs Supervisor on Ubuntu:

```bash
sudo apt-get install supervisor
```

#### Configuring Supervisor

Supervisor configuration files are stored in the **/etc/supervisor/conf.d** directory. Multiple files can be created in this directory to specify different programs, or multiple instances of the same program, for Supervisor to monitor. Copy this sample config into a file called **/etc/supervisor/conf.d/inengine-scheduler.conf**. 

```ini
[program:inengine-scheudler]
process_name=%(program_name)s_%(process_num)02d
directory=/path/to/scheduler
command=mono inengine.exe -s
autostart=true
autorestart=true
user=InEngine
numprocs=1
redirect_stderr=true
stdout_logfile=./scheduler.log
```

#### Starting Supervisor

Whenever a configuration change happens to files in the Supervisor config files, Supervisor needs to be instructed to reload its configuration.

```bash
sudo supervisorctl reread
sudo supervisorctl update
```

Now, simply start the InEngine Scheduler.

```bash
sudo supervisorctl start inengine-scheduler:*
```


