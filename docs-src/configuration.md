# Configuration 

Configuration is accomplished by modifying the appsettings.json file that comes with the InEngine.NET binary distribution. The -c, --configuration argument can also be used to specify an alternate configuration file.


```json
{
  "InEngine": {
    "Queue": {
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

| Setting                   | Description               |
| ------------------------- | ------------------------- |
| PrimaryQueueConsumers     | The number of consumers to schedule for the primary queue.        |
| SecondaryQueueConsumers   | The number of consumers to schedule for the secondary queue.      |
| QueueName                 | The base name of the queue, used to form the Redis Queue keys.    |
| RedisHost                 | The Redis hostname to connect to.                                 |
| RedisPort                 | Redis's port.                                                     |
| RedisDb                   | The Redis database - 0-15                                         |
| RedisPassword             | The Redis auth password                                           |



