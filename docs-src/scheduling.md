# Scheduling

[Commands](commands) can be scheduled to run by leveraging the InEngineScheduler.exe program, available as a download from a recent [release](https://github.com/InEngine-NET/InEngine.NET/releases).

## Scheduling a Command

A job schedule is created by adding a class to your plugin assembly that implements the **InEngine.Core.Jobs** interface.

```csharp

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
It will call the Jobs.Schedule method with an initialized Quartz.NET scheduler object.

```csharp
using System;
using Quartz;

namespace MyCommandPlugin
{
    public class Jobs : IJobs
    {
        public void Schedule(IScheduler scheduler)
        {
            var myCommand = new MyCommand();
            
            // Generate a schedulable job with the command.
            var job = myCommand.MakeTriggerBuilder().Build();
            
            // Generate a trigger for the job, and set its schedule to every 10 seconds.
            var trigger = myCommand.MakeTriggerBuilder().Build()
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever())
                .Build();
                
            // Register the job and trigger with the scheduler.
            scheduler.ScheduleJob(job, trigger);
        }
    }
}

```

## Running the Scheduler

### Manually from the CLI

Running the scheduler from the CommandLine is useful for debugging or local development. Simply run *InEngineScheduler.exe* from the command line.

```bash
InEngineScheduler.exe
```

It can also be run on Mac/Linux with Mono.

```bash
mono InEngineScheduler.exe
``` 

### On Windows as a Service


#### Installing
Run the Install.ps1 PowerShell script in the scheduler directory to install the scheduler in place. The script needs to be run as an administrator. The script will register the service at the location where the script is run. 

```bash
ps Install.ps1
```

#### Uninstalling

Simply run the **Uninstall.ps1** script with elevated permissions to unregister the service.

```bash
ps Uninstall.ps1
```

### On Linux with Supervisor

Supervisor is a process control system for Linux. It has extensive [documentation](http://supervisord.org/index.html), but the following should be enough to get started.

#### Installing Supervisor

This command installs Supervisor on Ubuntu:

```
sudo apt-get install supervisor
```

#### Configuring Supervisor

Supervisor configuration files are stored in the **/etc/supervisor/conf.d** directory. Multiple files can be created in this directory to specify different programs, or multiple instances of the same program, for Supervisor to monitor. Copy this sample config into a file called **/etc/supervisor/conf.d/inengine-scheduler.conf**. 

```ini
[program:inengine-scheudler]
process_name=%(program_name)s_%(process_num)02d
directory=/path/to/scheduler
command=mono InEngineScheduler.exe
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


