# Configuration 

Configuration is accomplished by modifying the appsettings.json file that comes with the InEngine.NET binary distribution.
The **-c, --configuration** argument can also be used to specify an alternate configuration file.


```json
{
  "InEngine": {
    "Plugins": [
      "path/to/MyCommandPlugin"
    ],
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

| Setting                   | Type              | Description                                                                                                           |
| ------------------------- | ----------------- | --------------------------------------------------------------------------------------------------------------------- |
| Plugins                   | array of strings  | A list of paths of plugin assemblies, with ".dll" omitted from the assembly name.                                     |
| ExecWhitelist             | object            | A set of key/value pairs, where the value is the file system path of an executable and the key is a command alias.    |


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
