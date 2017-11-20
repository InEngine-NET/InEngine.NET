# Commands

Commands are the fundamental abstraction used to run custom logic.

## Create a Command

The InEngine.Core package is required. Install it into your own Visual Studio project.

**Package Manager**
```bash
Install-Package InEngine.Core
```

**Nuget CLI**
```bash
nuget install InEgine.Core
```

**.NET CLI**
```bash
dotnet add package InEngine.Core
```

**Paket CLI**
```bash
paket add InEngine.Core
```

Adding a class that implements **InEngine.Core.ICommand** is the simplest way to create a command.

```csharp
using System;
using InEngine.Core;

namespace MyCommandPlugin
{
    public class MyCommand : ICommand
    {
        public CommandResult Run()
        {
            Console.WriteLine("Hello, world!");
            return new CommandResult(true);
        }
    }
}
```

Extending the **InEngine.Core.AbstractCommand** class adds extra functionality, like a logger, a progress bar, and the ability to schedule the command using the scheduler.
Minimally, the Run method should be overridden.

```csharp
using System;
using InEngine.Core;

namespace MyCommandPlugin
{
    public class MyCommand : ICommand
    {
        public override CommandResult Run()
        {
            Console.WriteLine("Hello, world!");
            return new CommandResult(true);
        }
    }
}
```


## Run a Command

Create a class that implements **InEngine.Core.IOptions** in the same assembly as the command class.
Add a VerbOptions attribute from the CommandLine namespace that defines the name of the command and optional help text.
The help text can be auto-generated from the attribute or manually specified in the GetUsage method.  

```csharp
using CommandLine;
using CommandLine.Text;
using InEngine.Core;

namespace MyCommandPlugin
{
    public class MyOptions : IOptions
    {
        [VerbOption("my-command", HelpText="My example command.")]
        public MyCommand MyCommand { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
```

Download the InEngineCli tool that matches the version of the InEngine.Core package you included from the [GitHub Releases](https://github.com/InEngine-NET/InEngine.NET/releases) page.

Copy your project's DLLs into the same directory as InEngineCli.exe.

Run your command...

```bash
InEngineCli.exe -pMyCommandPlugin my-command
```

## Discover Command Plugins

Run InEngineCli.exe without any arguments to see a list of arguments.

```text
Available plugins... 
InEngine.Commands
InEngine.Core
```

## Discover Commands in a Plugin

Run InEngineCli.exe with only the plugin specified.

```bash
InEngineCli.exe -pInEngine.Core
```

The **InEngine.Core** library is itself a plugin that contains queue related commands. 
As an example, this is the help output for the core plugin.

```text
InEngine 3.x
Copyright © Ethan Hann 2017

  queue:publish    Publish a command message to a queue.

  queue:consume    Consume one or more command messages from the queue.

  queue:length     Get the number of messages in the primary and secondary 
                   queues.

  queue:clear      Clear the primary and secondary queues.
```

## Print Help Text for a Plugin's Commands

Run the command with the -h or --help arguments.

```bash
InEngineCli.exe -pInEngine.Core queue:clear -h
```

The **InEngine.Core** plugin's command to clear the InEngine.NET's queues produces this help message. 

```text
InEngine 3.x
Copyright © Ethan Hann 2017

  --processing-queue    Clear the processing queue.

  --secondary           Clear the secondary queue.
```


## Logging

The **InEngine.Core.AbstractCommand** class provides a Logger property. It implements the **NLog.ILogger** interface.

```csharp
using System;
using InEngine.Core;

namespace MyCommandPlugin
{
    public class MyCommand : ICommand
    {
        public override CommandResult Run()
        {
            Logger.Trace("Sample trace message");
            Logger.Debug("Sample debug message");
            Logger.Info("Sample informational message");
            Logger.Warn("Sample warning message");
            Logger.Error("Sample error message");
            Logger.Fatal("Sample fatal error message");
            return new CommandResult(true);
        }
    }
}
```

Setup an [NLog configuration](https://github.com/NLog/NLog/wiki/Tutorial#configuration) file, something like this...

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

    <targets>
        <target name="logfile" xsi:type="File" fileName="file.txt" />
        <target name="console" xsi:type="Console" />
    </targets>

    <rules>
        <logger name="*" minlevel="Trace" writeTo="logfile" />
        <logger name="*" minlevel="Info" writeTo="console" />
    </rules>
</nlog>
```

## Progress Bar

The **InEngine.Core.AbstractCommand** class provides a ProgressBar property. This is how it is used.

```csharp
using InEngine.Core;

namespace MyCommandPlugin
{
    public class MyCommand : AbstractCommand
    {
        public override CommandResult Run()
        {
            // Define the ticks (aka steps) for the command...
            var maxTicks = 100000;
            SetProgressBarMaxTicks(maxTicks);

            // Do some work...
            for (var i = 0; i <= maxTicks;i++)
            {
                // Update the command's progress
                UpdateProgress(i);
            }

            return new CommandResult(true);
        }
    }
}
```
 
