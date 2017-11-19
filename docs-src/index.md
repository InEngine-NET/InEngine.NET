# What is this?

InEngine.NET is a background commands processing server. 
It allows commands to be queued, scheduled, and ran directly. 

## Install

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

Second, download the CLI and/or the Scheduler from a release that matches the version of the InEngine.Core package you included.

Releases are found on GitHub: https://github.com/InEngine-NET/InEngine.NET/releases

## Create a Command

Add a class that implements **InEngine.Core.ICommand** or extends **InEngine.Core.AbstractCommand**. 

```c#
using InEngine.Core;

namespace MyCommands
{
    public class MyCommand : ICommand
    {
        public CommandResult Run()
        {
            return new CommandResult(true);
        }
    }
}
```

The **AbstractCommand** class adds extra functionality, like a logger, a progress bar, and the ability to schedule the command using the scheduler.
Minimally, the Run method should be overridden.


```c#
using InEngine.Core;

namespace MyCommands
{
    public class MyCommand : AbstractCommand
    {
        public override CommandResult Run()
        {
            return new CommandResult(true);
        }
    }
}
```


 



