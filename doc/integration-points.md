---
layout: default
currentMenu: integration-points
---

# Integration Points

Integration points are services that are injected into [Integration Jobs](integration-jobs.html).

Available integration points are "Elasticsearch," "RabbitMQ," "JsonService," and "Mail."
They are [configured](configuration.html) in the IntegrationEngine.json file.

To create an integration point, create a class that extends an existing integration point class.
Note that an "integration point class" is one that implements _IntegrationEngine.Core.IntegrationPoint.IIntegrationPoint_.
Built-in integration point classes are the following.

* IntegrationEngine.Core.Elasticsearch.ElasticClientAdapter
* IntegrationEngine.Core.Mail.MailClient
* IntegrationEngine.Core.MessageQueue.RabbitMQClient
* IntegrationEngine.Core.ServiceStack.JsonService


The IntegrationPointName, defined in the configuration file, has to be the same name as the integration point's class name.

```csharp
using IntegrationEngine.Core.Mail;

namespace MyProject.MyIntegrationPoints
{
    public class MyMailClient : MailClient
    {
    }
}
```

```js
{
    // ...
    "IntegrationPoints": {
        "Mail": [
            {
                "IntegrationPointName": "MyMailClient", // Matches the class name.
                "HostName": "localhost",
                "Port": 25
            }
        ]
    }
    // ...
}
```
