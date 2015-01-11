---
layout: default
currentMenu: configuration
---

# Configuration

__InEngine.NET__ is configured with a JSON file called IntegrationEngine.json.
The configuration file has four sections: _WebApi_, _MessageQueue_, _Mail_, and _Database_.

### WebApi

The _WebApi_ section contains settings for the InEngine.NET's WebApi. 
Namely, the hostname and port of the web API. 

```
{
    // ...
    "WebApi": {
        "HostName": "localhost",
        "Port": 9001
    },
    // ...
}
```

### MessageQueue

The _MessageQueue_ section contains settings for connecting to a RabbitMQ server. 
If these settings are not correct, then a connection exception will be thrown when a job is triggered.

A queue called "myqueue" will not exist on a freshly installed RabbitMQ server. 
It will need to be created.

```
{
    // ...
    "MessageQueue": {
        "QueueName": "myqueue",
        "ExchangeName": "amq.direct",
        "UserName": "guest",
        "Password": "guest",
        "HostName": "localhost",
        "VirtualHost": "/"
    },
    // ...
}
```

### Elasticsearch

The _Elasticsearch_ section contains settings for connecting to a Elasticsearch server. 
If these settings are not correct, then a connection exception will be thrown when a job is scheduled.

```
{
    // ...
    "Elasticsearch": {
        "Protocol": "http",
        "HostName": "localhost",
        "Port": 9200,
        "DefaultIndex": "integration-engine"
    },
    // ...
}
```

### Mail

The _Mail_ section contains settings for connecting to an SMTP server.
If these settings are not correct, then a connection exception will be thrown when sending an email.

```
{
    // ...
    "Mail": {
        "HostName": "localhost",
        "Port": 25
    },
    // ...
}
```

### Database

The _Database_ section contains settings for connecting to a either a SQL Server or MySQL server via [Entity Framework](http://msdn.microsoft.com/en-us/data/ef.aspx).
If these settings are not correct, then a connection exception will be thrown when a SQL job runs.

```
{
    // ...
    "Database": {
        "ServerType": "SQLServer",
        "HostName": "localhost",
        "Port": 1433,
        "DatabaseName": "IntegrationEngine",
        "UserName": "inengine",
        "Password": "secret"
    }
    // ..
}
```

## Sample Configuration
This is a sample configuration.

```
{
    "WebApi": {
        "HostName": "localhost",
        "Port": 9001
    },
    "MessageQueue": {
        "QueueName": "myqueue",
        "ExchangeName": "amq.direct",
        "UserName": "guest",
        "Password": "guest",
        "HostName": "localhost",
        "VirtualHost": "/"
    },
    "Elasticsearch": {
        "Protocol": "http",
        "HostName": "localhost",
        "Port": 9200,
        "DefaultIndex": "integration-engine"
    },
    "Mail": {
        "HostName": "localhost",
        "Port": 25
    },
    "Database": {
        "ServerType": "SQLServer",
        "HostName": "localhost",
        "Port": 1433,
        "DatabaseName": "IntegrationEngine",
        "UserName": "inengine",
        "Password": "secret"
    }
}
```
