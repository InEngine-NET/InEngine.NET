# Configuration 

Configuration is accomplished by modifying the appsettings.json file that comes with the InEngine.NET binary distribution. The -c, --configuration argument can also be used to specify an alternate configuration file.


```json
{
  "InEngine": {
    "Plugins": [
      "path/to/MyCommandPlugin"
    ],
    "Queue": {
      "UseCompression": false,
      "PrimaryQueueConsumers":  16,
      "SecondaryQueueConsumers": 4,
      "QueueName": "InEngine:Queue",
      "RedisHost": "localhost",
      "RedisPort": 6379,
      "RedisDb": 0,
      "RedisPassword": ""
    }
  }
}

```


## Top-level Settings

| Setting                   | Type              | Description                                                                       |
| ------------------------- | ----------------- | --------------------------------------------------------------------------------- |
| Plugins                   | array of strings  | A list of paths of plugin assemblies, with ".dll" omitted from the assembly name. |


## Queue Settings

| Setting                   | Type      | Description                                                           |
| ------------------------- | --------- | --------------------------------------------------------------------- |
| UseCompression            | bool      | A situation performance optimization that compresses queued messages. |
| PrimaryQueueConsumers     | string    | The number of consumers to schedule for the secondary queue.          |
| SecondaryQueueConsumers   | string    | The number of consumers to schedule for the secondary queue.          |
| QueueName                 | string    | The base name of the queue, used to form the Redis Queue keys.        |
| RedisHost                 | string    | The Redis hostname to connect to.                                     |
| RedisPort                 | integer   | Redis's port.                                                         |
| RedisDb                   | integer   | The Redis database - 0-15                                             |
| RedisPassword             | string    | The Redis auth password                                               |
