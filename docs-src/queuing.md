# Queuing

## Prerequisites

Redis is required to use the InEngine.NET Queue feature. 
It can be installed on Ubuntu with this command:

```bash
sudo apt-get install redis-server
```

Start Redis with this command:

```bash
sudo service redis start
```

<div class="alert alert-info">
It is highly recommended to <a href="https://redis.io/topics/security#authentication-feature">set a password</a> for Redis.
</div>

## Working with Queues

### Publishing Commands

#### Programmatically

[Commands](commands) can be published programmatically with the **InEngine.Core.Queue.Broker** class:

```csharp
Broker.Make().Publish(new MyCommand());
```

#### From the Command Line
Commands can be published from the command line as well.
Note that all queue commands reside in the **InEngine.Core** plugin.
This is an example of how to publish a command from the CLI by specifying the commands assembly, class name, and arguments:

```bash
inengine.exe -pInEngine.Core queue:publish --command-assembly=MyCommandPlugin.dll --command-class=MyCommand --args "text=bar"
```

There is an "Echo" command in the *InEngine.Core* package. It is useful for end-to-end testing with the queue feature.
 
```bash
inengine.exe -pInEngine.Core queue:publish --command-assembly=InEngine.Core.dll --command-class=InEngine.Core.Commands.Echo --args "text=foo"
```

### Consuming Commands

#### Programmatically
Consuming a command is also accomplished with the Broker class:

```csharp
Broker.Make().Consume();
```

Both methods take an optional second argument to indicate if the secondary queue should be used instead of the primary queue.

```csharp
// Uses secondary queue.
Broker.Make().Consume(true);
```

Commands can be consumed from the command line as well with this simple command:

#### From the Command Line

```bash
inengine.exe -pInEngine.Core queue:consume
```

use the **--secondary** switch to consume the secondary queue instead of the primary queue:

```bash
inengine.exe -pInEngine.Core queue:consume --secondary
```

#### With the Scheduler

The InEngine scheduler is needed to consume queued messages in the background. 
There are a variety of [ways to run the scheduler](scheduling/#running-the-scheduler).
