# Scheduling

[Commands](commands) can be run at certain times on recurring schedules.

Scheduled commands are different from queued commands.
A command is queued when it is desirable to run the command once, as soon as possible.
Queued commands are typically configured right before being dispatched.
A command is scheduled when is it is desirable to run the command many times, on a definite schedule.
Scheduled commands are configured when their schedule is defined, in code. 

InEngine.NET takes a strictly programmatic approach to defining a schedule.
A command schedule is defined in code, not in an external data store.
There are several advantages to this approach.
One advantage is that the schedule can be versioned alongside the command code.
Another advantage is that there is no overhead from managing additional state in an external data store.

## Scheduling a Command

A job schedule is created by adding a class to a plugin assembly that extends the **InEngine.Core.AbstractPlugin** class.

This is a simple example:

```c#
using System;
using InEngine.Core;

namespace MyCommandPlugin
{
    public class MySchedulePlugin : AbstractPlugin
    {
        public override void Schedule(ISchedule schedule)
        {
            // Schedule some jobs
        }
    }
}
```

The InEngine.NET scheduler automatically discovers this class in your plugin assembly.
It will call the **AbstractPlugin.Schedule** method with an initialized **InEngine.Scheduling.Schedule** object.

This is a command schedule class with a few scheduling examples:

```c#
using System;
using InEngine.Core;

namespace MyCommandPlugin
{
    public class MySchedulePlugin : AbstractPlugin
    {
        public override void Schedule(ISchedule schedule)
        {
            /* 
             * Run MyCommand every five minutes. 
             */
            schedule.Command(new MyCommand()).EveryFiveMinutes();
            
            /* 
             * Run a lambda expression every ten minutes. 
             */
            schedule.Command(() => Console.WriteLine("Hello, world!")).EveryTenMinutes();
        }
    }
}
```

A command can be run on a custom cron schedule:

```c#
schedule.Command(new MyCommand()).Cron("15 * * * * ?");
```

There are a number of additional methods for scheduling commands to run on common intervals.

Run a command every second:

```c#
schedule.Command(new MyCommand()).EverySecond();
```

Run a command every minute:

```c#
schedule.Command(new MyCommand()).EveryMinute();
```

Run a command every 5 minutes:

```c#
schedule.Command(new MyCommand()).EveryFiveMinutes();
```

Run a command every 10 minutes:

```c#
schedule.Command(new MyCommand()).EveryTenMinutes();
```

Run a command every 15 minutes:

```c#
schedule.Command(new MyCommand()).EveryFifteenMinutes();
```

Run a command every 30 minutes:

```c#
schedule.Command(new MyCommand()).EveryThirtyMinutes();
```

Run a command hourly:

```c#
schedule.Command(new MyCommand()).Hourly();
```

Run a command hourly at a certain number of minutes past the hour (27 minutes in this example):

```c#
schedule.Command(new MyCommand()).HourlyAt(27);
```

Run a command daily:

```c#
schedule.Command(new MyCommand()).Daily();
```

Run a command daily at a specific time (at 10:30pm in this example):

```c#
schedule.Command(new MyCommand()).DailyAt(22, 30);
```

### In a Chain

A group of commands can be scheduled to run as an atomic batch.

This simple example schedules a group of imaginary file transfer commands to run in a chain:

```c#
schedule.Command(new[] {
    new MyFileTransfer(filePath1),
    new MyFileTransfer(filePath2),
    new MyFileTransfer(filePath3),
})
.Daily();
```

The chain of commands will stop executing if one of them fails.
The method **Failed** of the command that failed will be called.
This is a good place to add special logic that will allow the command to be recovered later, 
or to alert someone that manual intervention is necessary.  


### With Life Cycle Methods

Commands have optional life cycle methods that are initialized when a command is scheduled.

The **Before** and **After** methods allow for some custom logic to be called before and after the command is run:

```c#
schedule.Command(new Command())
    .EveryFiveMinutes()
    .Before(x => x.Info("X refers to the command. Its methods can be used."))
    .After(x => x.Info("X refers to the command"));
```

It is often useful to hit a URL before or after as well:

```c#
schedule.Command(new Command())
    .EveryFiveMinutes()
    .PingBefore("http://example.com")
    .PingAfter("http://example.com");
```

The **AbstractCommand** class has an instance of **InEngine.Core.IO.Write**. 
This class is more than just a wrapper for **Console.WriteLine**.

It also allows these life cycle methods to send the command's text output to files or an email:  

```c#
schedule.Command(new Command)
    .EveryFiveMinutes()
    .WriteOutputTo("/some/path")
    .AppendOutputTo("/some/path")
    .EmailOutputTo("example@inengine.net");
```

The **Before** and **After** methods allow for some custom logic to be called before and after the command is run:

```c#
schedule.Command(new Command)
    .EveryFiveMinutes()
    .Before(x => x.Info("X refers to the command. Its methods can be used."))
    .After(x => x.Info("X refers to the command"));
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

Run the **Install.ps1** PowerShell script in the scheduler directory to install the scheduler in place. 
The script needs to be run as an administrator. 
The script will register the service at the location where the script is run.

```bash
ps Install.ps1
```

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
[program:inengine]
process_name=%(program_name)s_%(process_num)02d
directory=/path/to/inengine
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

Now, simply start the scheduler workers with the **supervisorctl** program:

```bash
sudo supervisorctl start inengine:*
```

### In a Container with Docker

Install [Docker](https://www.docker.com/what-docker) first, then pull the **ethanhann/inengine** image:

```bash
docker pull ethanhann/inengine:latest
```

Now run the scheduler:

```bash
docker run --rm inengine -s
``` 
