---
layout: default
currentMenu: configuration
---

#Configuration

IntegrationEngine is configured with a JSON file called IntegrationEngine.json.
The configuration file has four sections: _WebApi_, _MessageQueue_, _Mail_, and _Database_.

###WebApi

The _WebApi_ section contains settings for the IntegrationEngine's WebApi. 
Namely, the hostname and port of the web API. 

###MessageQueue

The _MessageQueue_ section contains settings for connecting to a RabbitMQ server. 
If these settings are not correct, then a connection exception will be thrown when a job is scheduled. 

###Mail

The _Mail_ section contains settings for connecting to an SMTP server.
If these settings are not correct, then a connection exception will be thrown when sending an email.

###Database

The _Database_ section contains settings for connecting to a either a SQL Server or MySQL server via [Entity Framework](http://msdn.microsoft.com/en-us/data/ef.aspx).
If these settings are not correct, then a connection exception will be thrown when a SQL job runs.

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
