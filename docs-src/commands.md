# Commands

Commands can be C# classes, lambda expressions, or CLI programs.
They can be queued, scheduled, or from the command line.

## Create a Command

The InEngine.Core package is required to create a C# class command. 
Install it in a Visual Studio project.

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

To create a class command, extend the **InEngine.Core.AbstractCommand** class.
Minimally, the Run method should be overridden.

```c#
using System;
using InEngine.Core;

namespace MyCommandPlugin
{
    public class MyCommand : AbstractCommand
    {
        public override void Run()
        {
            Console.WriteLine("Hello, world!");
        }
    }
}
```

## Run a Command

Create a class that extends **InEngine.Core.AbstractPlugin** in the same assembly as the command class.
Add a VerbOptions attribute, from the CommandLine namespace, that defines the name of the command. 

```c#
using CommandLine;
using CommandLine.Text;
using InEngine.Core;

namespace MyCommandPlugin
{
    public class MyOptions : AbstractPlugin
    {
        [VerbOption("my-command", HelpText="My example command.")]
        public MyCommand MyCommand { get; set; }
    }
}
```

Download the InEngine binary distribution, from the [GitHub Releases](https://github.com/InEngine-NET/InEngine.NET/releases) page, that matches the version of the InEngine.Core package you included.

Copy your project's DLLs into the Plugins subdirectory included in the binary distribution. 
Add your plugin to the ["Plugins" list in appsettings.config](configuration) at the root of the binary distribution.

Run your command:

```bash
inengine.exe -pMyCommandPlugin my-command
```

### Writing Output

The **InEngine.Core.AbstractCommand** class provides some helper functions to output text to the console, for example:

```c#
public override void Run()
{
    Line("Display some information");
}
```

All of these commands append a newline to the end of the specified text:

```c#
Line("This is some text");                  // Text color is white
Info("Something good happened");            // Text color is green
Warning("Something not so good happened");  // Text color is yellow
Error("Something bad happened");            // Text color is red
```

These commands are similar, but they do not append a newline:

```c#
Text("This is some text");                      // Text color is white
InfoText("Something good happened");            // Text color is green
WarningText("Something not so good happened");  // Text color is yellow
ErrorText("Something bad happened");            // Text color is red
```

You can also display newlines:
 
```c#
Newline();      // 1 newline
Newline(5);     // 5 newlines
Newline(10);    // 10 newlines
```

The methods can be chained together:

```c#
InfoText("You have this many things: ")
    .Line("23")
    .NewLine(2)
    .InfoText("You have this many other things: ")
    .Line("34")
    .NewLine(2); 
```

### Progress Bar

The **InEngine.Core.AbstractCommand** class provides a ProgressBar property to show command progress in a terminal.
This is how it is used:

```c#
public override void Run()
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
}
```

### Executing Arbitrary Processes

It isn't necessary to create C# classes to utilize InEngine.NET.
Arbitrary commands can be run, with an argument list by leveraging the InEngine.Core plugin's **proc** command.
The command lists directory contents using "ls" with the "-lhp" switches:

```bash
inengine.exe -pInEngine.Core proc -c"/bin/ls" -a"-lhp"
```

## View Available Plugins

Run inengine.exe without any arguments to see a list of plugins:

```text
  ___       _____             _              _   _ _____ _____ 
 |_ _|_ __ | ____|_ __   __ _(_)_ __   ___  | \ | | ____|_   _|
  | || '_ \|  _| | '_ \ / _` | | '_ \ / _ \ |  \| |  _|   | |  
  | || | | | |___| | | | (_| | | | | |  __/_| |\  | |___  | |  
 |___|_| |_|_____|_| |_|\__, |_|_| |_|\___(_|_| \_|_____| |_|  
                        |___/ 

Usage:
InEngine 3.x
Copyright © 2017 Ethan Hann

  p, plugin           Plug-In to activate.

  s, scheduler        Run the scheduler.

  c, configuration    (Default: ./appsettings.json) The path to the 
                      configuration file.


Plugins:
  InEngine.Core
```

## View Commands in a Plugin

Run inengine.exe with only the plugin specified:

```bash
inengine.exe -pInEngine.Core
```

The **InEngine.Core** library is itself a plugin that contains queue-related and other commands. 
As an example, this is the help output for the core plugin.

```text
  ___       _____             _              _   _ _____ _____ 
 |_ _|_ __ | ____|_ __   __ _(_)_ __   ___  | \ | | ____|_   _|
  | || '_ \|  _| | '_ \ / _` | | '_ \ / _ \ |  \| |  _|   | |  
  | || | | | |___| | | | (_| | | | | |  __/_| |\  | |___  | |  
 |___|_| |_|_____|_| |_|\__, |_|_| |_|\___(_|_| \_|_____| |_|  
                        |___/ 

Plugin: 
  Name:    InEngine.Core
  Version: 3.x


Commands:
  queue:publish     Publish a command message to a queue.
  queue:consume     Consume one or more command messages from the queue.
  queue:length      Get the number of messages in the primary and secondary queues.
  queue:flush       Clear the primary or secondary queues.
  queue:republish   Republish failed messages to the queue.
  queue:peek        Peek at messages in the primary or secondary queues.
  echo              Echo some text to the console. Useful for end-to-end testing.
  proc              Launch an arbitrary process.
```

## Print Help Text for a Plugin's Commands

Run the command with the -h or --help arguments.

```bash
inengine.exe -pInEngine.Core queue:publish -h
```

The **InEngine.Core** plugin's command to clear the InEngine.NET queues produces this help message. 

```text
InEngine 3.x
Copyright © 2017 Ethan Hann

  --command-plugin    Required. The name of a command plugin file, e.g. 
                      InEngine.Core

  --command-verb      A plugin command verb, e.g. echo

  --command-class     A command class name, e.g. 
                      InEngine.Core.Commands.AlwaysSucceed. Takes precedence 
                      over --command-verb if both are specified.

  --args              An optional list of arguments to publish with the 
                      command.

  --secondary         (Default: False) Publish the command to the secondary 
                      queue.
```

## Logging

Any exceptions thrown by a command will be logged provided NLog is configured to record errors. 
The [NLog configuration](https://github.com/NLog/NLog/wiki/Tutorial#configuration) file needs to be setup with something like this: 

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

    <targets>
        <target name="logfile" xsi:type="File" fileName="inengine.log" />
    </targets>

    <rules>
        <logger name="*" minlevel="Error" writeTo="logfile" />
    </rules>
</nlog>
```
