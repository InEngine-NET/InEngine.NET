InEngine.NET is a plugin-based software application that allows [commands](commands) to be [queued](queuing), [scheduled](scheduling), and run directly.

## How does it work?

InEngine.NET uses a plugin system to dynamically load .NET assemblies and execute code. 
It also has a built-in command for launching external non-.NET programs.

Get started by pulling the binaries from the [latest release](https://github.com/InEngine-NET/InEngine.NET/releases) on GitHub.

Then run the **echo** command from the core plugin:

```bash
inengine.exe echo --text"Hello, world"
```
Or if you're a Linux or Mac OS X fan (like me!), use the **inengine** shell script ([Mono](http://www.mono-project.com/download/) is required):

```bash
inengine echo --text"Hello, world"
```

Instead of downloading binaries and runtimes, you can pull the latest Docker image:

```bash
docker pull ethanhann/inengine:latest
```

Now run a command in a container:

```bash
docker run --rm inengine echo --text"Hello, world"
``` 

## How does queueing work?

Want to queue our example echo command to run in the background or possibly on another server?

Use the core plugin's **queue:publish** command:

```bash
inengine.exe queue:publish --plugin=InEngine.Core --command=echo --args "text=Hello, world"
``` 

How do we consume that queued echo command?

Use the core plugin's **queue:consume** command:

```bash
inengine.exe queue:consume
``` 

## How do I run non-.NET commands?

There is a special **exec** command in the core plugin that allows for the execution of any program you can run at the command line. 

For example, create a python script called **helloworld.py**, make it executable, and add this to it:

```python
#!/usr/bin/env python

print 'Hello, world!'
```

Whitelist the "helloworld" executable in the [appsettings.json](configuration) file:

```json
{
  "InEngine": {
    // ...
    "ExecWhitelist": {
      "helloworld": "/path/to/helloworld.py"
    }
    // ...
  }
}
```

Now execute it with the **exec** command:

```bash
inengine exec --executable="helloworld"
```

Why would you want to do this?
It opens up the possibility of running shell scripts, ETLs, Java programs, etc. in the background or on a schedule. 

The example python script can be queued:

```bash
inengine queue:publish --plugin=InEngine.Core --command=exec --args="executable=helloworld"
```
