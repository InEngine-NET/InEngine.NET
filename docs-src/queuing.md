# Queuing

InEngine.NET's queue functionality allows for commands to be run in the background with a simple publish/consume model. 

## Queue Drivers

To make use of queue features, a queue driver must be specified in [appsettings.json](configuration).
These are the available drivers...

### File

The file driver writes queued messages to the file system. 
It is useful for testing and development, but probably not suitable for production.

### Redis

Redis is suitable for production use. 
InEngine.NET utilizes Redis' durable queue features which mean messages will not be lost if InEngine.NET unexpectedly fails. 

Redis can be installed on Ubuntu with this command:

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

### Sync

The sync driver causes the publish command to run a published command synchronously.
All other queue commands and methods are not supported and will throw an exception if called.
This driver can be useful for plugin development and testing.

## Publishing Commands

### With C# Classes

[Commands](commands) can be published programmatically with the **InEngine.Core.Queuing.Queue** class:

```c#
Queue.Make().Publish(new MyCommand());
```

Or publish to the secondary queue by passing true to the Make method:

```c#
Queue.Make(true).Publish(new MyCommand());
```

!!! note "Do I have to use Queue.Make()?"
    Queue.Make() is a factory method that autoloads the queue settings from appsettings.json, creates the appropriate queue driver, and returns an instance of Queue.
    You can create your own Queue object an initialize it if you want.
    At the very least you can assign the object returned by Queue.Make() to a local variable or load it into a DI container for later use.

### With Lambda Expressions

Lambda expressions, aka anonymous functions, can be queued.
The disadvantage to queuing lambdas is that the helpful functionality available in **InEngine.Core.AbstractCommand** is not available.  

This is how you queue a lambda:

```c#
Queue.Make().Publish(() => Console.WriteLine("Hello, world!"));
```

Here is a neat shortcut for commands without parameters:

```c#
Queue.Make().Publish(() => Foo.Bar());
// Can be rewritten as...
Queue.Make().Publish(Foo.Bar);
```

### Sequentially In a Chain

Chained commands run in the order specified.
This is useful for when order matters.

Also, if one command in the chain fails, then subsequent commands are not run at all.
This affords the opportunity to add additional code that records which command failed, then resuming the command chain where it left off.

Here is a an example of how to chain a series of (imaginary) file transfer commands together:

```c#
Subject.Publish(new[] {
    new MyFileTransfer(filePath1),
    new MyFileTransfer(filePath2),
    new MyFileTransfer(filePath3),
});
```


```c#
Subject.Publish(new List<AbstractCommand>() {
    new AlwaysSucceed(),
    new Echo() { VerbatimText = "Hello, world!"},
});
```

### From the Command Line

Commands can be published from the command line as well.
Note that all queue commands reside in the **InEngine.Core** plugin.
This is an example of how to publish a command from the CLI by specifying the command's plugin, class name, and arguments:

```bash
inengine.exe -pInEngine.Core queue:publish --command-plugin=MyCommandPlugin.dll --command-class=MyCommand --args "text=bar"
```

There is an "Echo" command in the *InEngine.Core* package. It is useful for end-to-end testing with the queue feature.
 
```bash
inengine.exe -pInEngine.Core queue:publish --command-plugin=InEngine.Core.dll --command-class=InEngine.Core.Commands.Echo --args "text=foo"
```

The command verb can also be specified instead of the full class name:
 
```bash
inengine.exe -pInEngine.Core queue:publish --command-plugin=InEngine.Core.dll --command-verb=echo--args "text=foo"
```

## Consuming Commands

### From Code
Consuming a command is also accomplished with the Queue class:

```c#
Queue.Make().Consume();
```

The make method takes an optional second argument to indicate if the secondary queue should be used instead of the primary queue.

```c#
// Uses secondary queue.
Queue.Make(true).Consume();
```

Commands can be consumed from the command line as well with this simple command:

### From the Command Line

```bash
inengine.exe -pInEngine.Core queue:consume
```

Use the **--secondary** argument to consume the secondary queue instead of the primary queue:

```bash
inengine.exe -pInEngine.Core queue:consume --secondary
```

### With the Scheduler

The InEngine scheduler is needed to consume queued messages in the background. 
There are a variety of [ways to run the scheduler](scheduling/#running-the-scheduler).

## Examining the Queue

### Viewing Queue Lengths

The **queue:length** command shows a quick summary of pending, in-progress, and failed commands in the primary and secondary queues:

```bash
inengine.exe -pInEngine.Core queue:length
```

### Peek at Queued Commands

The **queue:peek** command allows for queued commands to be inspected:

```bash
inengine.exe -pInEngine.Core queue:peek --pending --in-progress --failed
```  

It is of course possible to peek in the secondary queues:

```bash
inengine.exe -pInEngine.Core queue:peek --pending --secondary
```

Queued commands can be viewed in JSON which maybe useful for debugging:

```bash
inengine.exe -pInEngine.Core queue:peek --pending --json
```  

By default, up to the first 10 messages will be retrieved, but the range is configurable:

```bash
inengine.exe -pInEngine.Core queue:peek --pending --to=100
```

A slice of the queue can be retrieved using the from argument.
For example, this queue:peek call retrieves the 100-200 queued commands:

```bash
inengine.exe -pInEngine.Core queue:peek --pending --from=100 --to=200
```

## Handling Failed Commands

Commands that throw an exception are put in a special "failed" queue. 
They can be republished with the **queue:republish** command:

```bash
inengine.exe -pInEngine.Core queue:republish
```

Failed secondary queue commands can be republished as well:

```bash
inengine.exe -pInEngine.Core queue:republish --secondary
```

By default, only 100 failed commands are republished at a time.
The is configurable:

```bash
inengine.exe -pInEngine.Core queue:republish --limit=1000
```

## Primary and Secondary Queue

Other than the fact that the primary queue is used by default, there is no difference between the primary and secondary queues. 
However, it is often desirable to use two queues. 
For example, long running jobs might be sent to the secondary queue, 
while jobs that are expected to finish after only a few moments are sent to the primary queue.

What about 3, 4, or 900 queues? Managing numerous queues gets to be a pain and, practically speaking, is probably unnecessary.
If it is desirable, different [configuration files](configuration) can be used to run multiple instances of InEngine.NET.
Simply create a new config file with a new QueueName setting and point inengine.exe at it with the -c argument:

```bash
inengine.exe -cMyCustomSettingsFile.json -pInEngine.Core queue:consume
```

## Message Compression

Messages can be compressed when saved in the queue. 
It is important to understand the trade-offs of this feature before enabling it.
If the queued commands are too small to benefit from being compressed, then compressing them wastes resources.
Compressing messages takes more CPU resources and might negatively impact queue throughput if the queued commands do not have a lot of internal state.
 
If the commands have a lot of internal state, then this feature will reduce the queue's memory consumption.
Also, in a high-throughput scenario, where network bandwidth is limited, this feature can greatly reduce the amount of bandwidth used.

