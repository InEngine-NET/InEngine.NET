InEngine.NET allows commands to be [queued](queuing), [scheduled](scheduling), and run directly.

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

Want to queue our example echo command to run in (possibly) another server?

Use the core plugin's **queue:publish** command:

```bash
inengine.exe -pInEngine.Core queue:publish --command-plugin=InEngine.Core.dll --command-verb=echo --args "text=Hello, world"
``` 

How do we consume that queued echo command?

Use the core plugin's **queue:consume** command of course:

```bash
inengine.exe -pInEngine.Core queue:consume
``` 
