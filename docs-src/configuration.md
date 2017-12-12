# Configuration 

## InEngine Settings
Configuration is accomplished by modifying the appsettings.json file that comes with the InEngine.NET binary distribution.
The **-c, --configuration** argument can also be used to specify an alternate configuration file.


```json
{
  "InEngine": {
    "Plugins": {
      "MyPlugin": "/path/to/plugin/assembly"
    },
    "ExecWhitelist": {
      "foo": "/path/to/foo.exe"
    },
    "Mail": {
      "Host": "localhost",
      "Port": 25,
      "From": "no-reply@inengine.net"
    },
    "Queue": {
      "UseCompression": false,
      "PrimaryQueueConsumers":  16,
      "SecondaryQueueConsumers": 4,
      "QueueDriver": "redis",
      "QueueName": "InEngineQueue",
      "RedisHost": "localhost",
      "RedisPort": 6379,
      "RedisDb": 0,
      "RedisPassword": ""
    }
  }
}

```


## Top-level Settings

| Setting                   | Type              | Description                                                                                                                                |
| ------------------------- | ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| Plugins                   | object            | A set of key/value pairs, where the value is the directory where the plugin is located and the key is the plugin name sans .dll extension. |
| ExecWhitelist             | object            | A set of key/value pairs, where the value is the file system path of an executable and the key is a command alias.                         |


## Mail Settings

| Setting   | Type      | Description                                           |
| --------- | --------- | ----------------------------------------------------- |
| Host      | string    | The hostname of an SMTP server.                       |
| Port      | integer   | The port of an SMTP server.                           |
| From      | string    | The default email address used to send email from.    |


## Queue Settings

| Setting                   | Type      | Description                                                           |
| ------------------------- | --------- | --------------------------------------------------------------------- |
| UseCompression            | bool      | A situation performance optimization that compresses queued messages. |
| PrimaryQueueConsumers     | string    | The number of consumers to schedule for the secondary queue.          |
| SecondaryQueueConsumers   | string    | The number of consumers to schedule for the secondary queue.          |
| QueueDriver               | string    | The driver to use to interact with a queue data store.                |
| QueueName                 | string    | The base name of the queue, used to form the Redis Queue keys.        |
| RedisHost                 | string    | The Redis hostname to connect to.                                     |
| RedisPort                 | integer   | Redis's port.                                                         |
| RedisDb                   | integer   | The Redis database - 0-15                                             |
| RedisPassword             | string    | The Redis auth password                                               |


## Logging Settings

Any exceptions thrown by a command will be logged, provided NLog is configured to log exceptions. 
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

InEngine.Core does not depend explicitly on NLog, but rather [Common.Logging](http://net-commons.github.io/common-logging/).
This means that any logging framework that Common.Logging supports can be used.
Configuring Common.Logging to use a different logging framework is out of the scope of this documentation.


