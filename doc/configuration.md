---
layout: default
currentMenu: configuration
---

# Configuration

__InEngine.NET__ is configured with a JSON file called IntegrationEngine.json.
The configuration file has several sections, outlined below.

## WebApi

The _WebApi_ section contains settings for the InEngine.NET's WebApi. 
Namely, the hostname and port of the web API.
A list of CORS origins can also be specified.
This is useful when accessing the API via the browser with the [InEngine.NET Dashboard](https://github.com/ethanhann/InEngine.NET-Dashboard).
Note that it is not recommended to use "*" wildcard, but a list of specific domains is preferred instead.

```js
{
    // ...
    "WebApi": {
        "HostName": "localhost",
        "Port": 9001,
        "Origins": ["*"]
    },
    // ...
}
```

## NLogAdapter

The _NLogAdapter_ section contains settings for configuring the [NLog Common.Logging adapter](http://netcommon.sourceforge.net/docs/2.1.0/reference/html/ch01.html#logging-adapters-nlog).

```js
{
    // ...
    "NLogAdapter": {
		"ConfigType": "File",
		"ConfigFile": "IntegrationEngine.nlog.xml"
    }
    // ..
}
```

## Integration Points

Any number of integration points [Integration Points](integration-points.html) can be configured.
All integration points should have a unique name defined in its _IntegrationPointName_ property. 

### RabbitMQ

The _RabbitMQ_ section contains settings for connecting to a RabbitMQ server. 
If these settings are not correct, then a connection exception will be thrown when a job is triggered.

A queue called "myqueue" will not exist on a freshly installed RabbitMQ server. 
It will need to be created.

```js
{
    // ...
    "IntegrationPoints": {
        "RabbitMQ": [
            {
                "IntegrationPointName": "DefaultRabbitMQ",
                "QueueName": "myqueue",
                "ExchangeName": "amq.direct",
                "UserName": "inengine",
                "Password": "secret",
                "HostName": "localhost",
                "VirtualHost": "/"
            }
        ],
    }
    // ...
}
```

### Elasticsearch

The _Elasticsearch_ section contains settings for connecting to a Elasticsearch server. 
If these settings are not correct, then a connection exception will be thrown when a job is scheduled.

```js
{
    // ...
    "IntegrationPoints": {
        "Elasticsearch": [
            {
                "IntegrationPointName": "DefaultElasticsearch",
                "Protocol": "http",
                "HostName": "localhost",
                "Port": 9200,
                "DefaultIndex": "integration-engine"
            }
        ]
    }
    // ...
}
```

### Mail

The _Mail_ section contains settings for connecting to an SMTP server.
If these settings are not correct, then a connection exception will be thrown when sending an email.

```js
{
    // ...
    "IntegrationPoints": {
        "Mail": [
            {
                "IntegrationPointName": "DefaultMail",
                "HostName": "localhost",
                "Port": 25
            }
        ]
    }
    // ...
}
```

## Sample Configuration
This is a sample minimum configuration.

```js
{
    "WebApi": {
        "HostName": "localhost",
        "Port": 9001,
        "Origins": ["*"]
    },
    "NLogAdapter": {
        "ConfigType": "File",
        "ConfigFile": "IntegrationEngine.nlog.xml"
    },
    "IntegrationPoints": {
        "RabbitMQ": [
            {
                "IntegrationPointName": "DefaultRabbitMQ",
                "QueueName": "myqueue",
                "ExchangeName": "amq.direct",
                "UserName": "inengine",
                "Password": "secret",
                "HostName": "localhost",
                "VirtualHost": "/"
            }
        ],
        "Elasticsearch": [
            {
                "IntegrationPointName": "DefaultElasticsearch",
                "Protocol": "http",
                "HostName": "localhost",
                "Port": 9200,
                "DefaultIndex": "integration-engine"
            }
        ]
    }
}
```
