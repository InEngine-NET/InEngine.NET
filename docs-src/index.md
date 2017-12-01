InEngine.NET allows commands to be queued, scheduled, and run directly.

## How does it work?

InEngine.NET uses a plugin system to dynamically load .NET assemblies and execute code. 
It also has a built-in command for launching external non-.NET programs.

Get started by pulling the latest binaries from the [latest release](https://github.com/InEngine-NET/InEngine.NET/releases) on GitHub.

Then run a [command](commands):

```bash
inengine.exe -pInEngine.Core echo --text"Hello, world"
```
Or if you\'re a Linux or Mac OS X fan (like me!), use the **inengine** shell script ([Mono](http://www.mono-project.com/download/) required.):

```bash
# or if you\'re a Linux fan like me... 

inengine -pInEngine.Core echo --text"Hello, world"
```

Instead of downloading binaries and runtimes, you can pull the latest Docker image:

```bash
docker pull ethanhann/inengine:latest
```

Now run a command in a container:

```bash
docker run --rm inengine -pInEngine.Core echo --text"Hello, world"
``` 

You can get started with either the  
Get started by reading up on [commands](commands), then [scheduling](scheduling) and [queuing](queuing).

