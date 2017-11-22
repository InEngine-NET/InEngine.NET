# Configuration 

Configuration is accomplished by modifying the appsettings.json file that comes with the InEngine.NET binary distribution.


```json
{
  "InEngine": {
    "Queue": {
      "QueueName": "InEngine:Queue",
      "RedisHost": "localhost",
      "RedisPort": 6379,
      "RedisDb": 0,
      "RedisPassword": ""
    }
  }
}
```

The -c, --configuration argument can also be used to specify an alternate configuration file.




