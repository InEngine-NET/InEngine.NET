# Queuing

InEngine.NET's queue functionality allows for commands to be run in the background via a [publish/subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) model.
A command should be queued when it is desirable to run the command once, as soon as possible, in a background task.

Queuing is especially important for Web sites and applications. 
Queuing is like the sister technology of caching.
Caching makes page loads faster when reading data from a database, or serving static assets, by drastically reducing direct database and file system read operations. 
Queuing makes page loads faster when writing data to a database, or performing other blocking tasks, by pushing them into the background.
Some common examples are sending emails, importing CSV files into a database, transforming an image file, and processing a shopping cart order.
It is not appropriate to use queuing if output from the task is needed immediately in the response of a Web request.

## Queue Drivers

To make use of queue features, a queue driver must be specified in [appsettings.json](configuration).

These are the available drivers...

### File

The file driver writes queued messages to the file system. 
It is useful for testing and development, but probably not suitable for production.

### Redis

Redis is suitable for production use. 
InEngine.NET utilizes Redis' durable queue features which means messages will not be lost if InEngine.NET unexpectedly fails. 

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

### RabbitMQ

RabbitMQ has built-in acknowledgement and persistence features that make it ideal for production use. 

RabbitMQ can be installed on all major platforms, including Windows.

See the [download and installation](https://www.rabbitmq.com/download.html) page to get started.


### Sync

The sync driver causes the publish command to run a published command synchronously.
All other queue commands and methods are not supported and will throw an exception if called.
This driver can be useful for plugin development and testing.

## Enqueuing Commands

### With C# Classes

[Commands](commands) can be enqueued programmatically with the **InEngine.Core.Queuing.Enqueue** class:

```c#
Enqueue.Command(new MyCommand())
       .Dispatch();
```

Or enqueue to the secondary queue:

```c#
Enqueue.Command(new MyCommand())
       .ToSecondaryQueue()
       .Dispatch();
```

Enqueue is just a wrapper.
It is possible to peel back the covers to get to the queue client.
```c#
// Enqueue.Command actually returns a life cycle object...
var commandToDispatch = Enqueue.Command(new MyCommand());

// The life cycle object has a QueueAdapter that can be set...
var shouldUseSecondaryQueue = true;
commandToDispatch.QueueAdapter = QueueAdapter.Make(shouldUseSecondaryQueue);

// Now dispatch the command... 
commandToDispatch.Dispatch();
```

### With Lambda Expressions

Lambda expressions, aka anonymous functions, can be queued.
The disadvantage to queuing lambdas is that the helpful functionality available in **InEngine.Core.AbstractCommand** is not available.  

This is how you queue a lambda:

```c#
Enqueue.Command(() => Console.WriteLine("Hello, world!"))
       .Dispatch();
```

### With the "exec" Command

The **exec** command allows for external programs to be executed.

```bash
inengine queue:publish --plugin=InEngine.Core --command=exec --args="command=/usr/bin/python" "args=--version"
```

!!! note "Do not include "--" for the command and args parameters."
    This is purely to make parsing easier internally.


    
### Sequentially In a Chain

Chained commands run in the order specified.
This is useful for when order matters.

Also, if one command in the chain fails, then subsequent commands are not run at all.
This affords the opportunity to add additional code that records which command failed to notify someone that manual intervention is necessary, 
or some other error handling functionality.

Here is a an example of how to chain a series of (imaginary) file transfer commands together:

```c#
Enqueue.Command(new[] {
            new MyFileTransfer(filePath1),
            new MyFileTransfer(filePath2),
            new MyFileTransfer(filePath3),
       })
       .Dispatch();
```

It is also possible to enqueue a list of different commands:
 
```c#
Enqueue.Command(new List<AbstractCommand>() {
           new AlwaysSucceed(),
           new Echo() { VerbatimText = "Hello, world!"},
       })
        .Dispatch();
```

### With Life Cycle Methods

Commands have optional life cycle methods that are initialized when a command is enqueued.
This is similar to scheduling life cycle methods, but a queue does not have the **Before** and **After** method.
It has the rest.

It is often useful to hit a URL before or after the command runs:

```c#
Enqueue.Command(new Command())
    .EveryFiveMinutes()
    .PingBefore("http://example.com")
    .PingAfter("http://example.com");
```

The **AbstractCommand** class has an instance of **InEngine.Core.IO.Write**. 
This class is more than just a wrapper for **Console.WriteLine**.

It also allows these life cycle methods to send the command's text output to files or an email:  

```c#
Enqueue.Command(new Command)
    .EveryFiveMinutes()
    .WriteOutputTo("/some/path")
    .AppendOutputTo("/some/path")
    .EmailOutputTo("example@inengine.net");
``` 

### From the Command Line

Commands can be published from the command line as well.
Note that all queue commands reside in the **InEngine.Core** plugin.
This is an example of how to publish a command from the CLI by specifying the command's plugin, class name, and arguments:

```bash
inengine.exe queue:publish --plugin=MyCommandPlugin --class=MyCommand --args "text=bar"
```

There is an "Echo" command in the *InEngine.Core* package. It is useful for end-to-end testing with the queue feature.
 
```bash
inengine.exe queue:publish --plugin=InEngine.Core --class=InEngine.Core.Commands.Echo --args "text=foo"
```

The command verb can also be specified instead of the full class name:
 
```bash
inengine.exe queue:publish --plugin=InEngine.Core --command=echo--args "text=foo"
```

## Consuming Commands

### From the Command Line

Commands can be consumed from the command line with this simple command:

```bash
inengine.exe queue:consume
```

Use the **--secondary** argument to consume the secondary queue instead of the primary queue:

```bash
inengine.exe queue:consume --secondary
```

### With the Scheduler

The InEngine scheduler is needed to consume queued messages in the background. 
There are a variety of [ways to run the scheduler](scheduling/#running-the-scheduler).

### From Code

It should (probably) never be necessary to manually consume commands in code, but it is possible. 

Consuming a command is accomplished with the **InEngine.Core.Queuing.Commands.Consume** class:

```c#
new Consume().Run();
```

Or consume the secondary queue:

```c#
new Consume {
    UseSecondaryQueue = true
}.Run();
```

## Examining the Queue

### Viewing Queue Lengths

The **queue:length** command shows a quick summary of pending, in-progress, and failed commands in the primary and secondary queues:

```bash
inengine.exe queue:length
```

### Peek at Queued Commands

The **queue:peek** command allows for queued commands to be inspected:

```bash
inengine.exe queue:peek --pending --in-progress --failed
```  

It is of course possible to peek in the secondary queues:

```bash
inengine.exe queue:peek --pending --secondary
```

Queued commands can be viewed in JSON which maybe useful for debugging:

```bash
inengine.exe queue:peek --pending --json
```  

By default, up to the first 10 messages will be retrieved, but the range is configurable:

```bash
inengine.exe queue:peek --pending --to=100
```

A slice of the queue can be retrieved using the from argument.
For example, this queue:peek call retrieves the 100-200 queued commands:

```bash
inengine.exe queue:peek --pending --from=100 --to=200
```

## Re-queuing Failed Commands

Commands that throw an exception are put in a special "failed" queue. 
They can be republished with the **queue:republish** command:

```bash
inengine.exe queue:republish
```

Failed secondary queue commands can be republished as well:

```bash
inengine.exe queue:republish --secondary
```

By default, only 100 failed commands are republished at a time.
The is configurable:

```bash
inengine.exe queue:republish --limit=1000
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
inengine.exe -cMyCustomSettingsFile.json queue:consume
```

## Message Compression

Messages can be compressed when saved in the queue. 
It is important to understand the trade-offs of this feature before enabling it.
If the queued commands are too small to benefit from being compressed, then compressing them wastes resources.
Compressing messages takes more CPU resources and might negatively impact queue throughput if the queued commands do not have a lot of internal state.
 
If the commands have a lot of internal state, then this feature will reduce the queue's memory consumption.
Also, in a high-throughput scenario, where network bandwidth is limited, this feature can greatly reduce the amount of bandwidth used.

