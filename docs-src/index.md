# What is this?

InEngine.NET is a background commands processing server. 
It allows commands to be queued, scheduled, and ran directly. 

## How to Get Started

### Create a Command

First, install the InEngine.Core package into your own Visual Studio project.

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

Add a class that implements **InEngine.Core.ICommand** or extends **InEngine.Core.AbstractCommand**. 
The **AbstractCommand** class adds extra functionality, like a logger, a progress bar, and the ability to schedule the command using the scheduler.
Minimally, the Run method should be overridden.

```csharp
using System;
using InEngine.Core;

namespace MyCommands
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

Create a class that implements **InEngine.Core.IOptions** in the same assembly as the command class.
Add VerbOptions from the CommandLine namespace.
The help text can be auto-generated or manually specified.  

```csharp
using CommandLine;
using CommandLine.Text;
using InEngine.Core;

namespace InEngine.Commands
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

### Run the Command

Second, download the InEngineCli tool that matches the version of the InEngine.Core package you included from the [GitHub Releases](https://github.com/InEngine-NET/InEngine.NET/releases) page.

Copy your project's DLLs into the same directory as InEngineCli.exe.

Run your command...

```bash
InEngineCli.exe -pMyCommands my-command
```









