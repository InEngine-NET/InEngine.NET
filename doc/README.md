---
layout: default
currentMenu: home
---

#Welcome

__IntegrationEngine__ is a set of .NET packages, created by [Ethan Hann](http://ethanhann.com), that allows for the 
creation of a code-centric data integration and asynchronous job scheduling server.
 
##Why should you care?

Data integration is a common task when working in a large or medium enterprise environment.
Data integration servers take data from a data source, transform it (or not), and put it somewhere. 
__IntegrationEngine__ can allow you to do that and more in a programmatic manner.

There are several existing full-featured data integration server products on the market.
These products do not work in a way software developers are comfortable with.
They provide a drag-and-drop GUI application for implementing a data integration.
This sounds appealing at the surface, but the GUIs are often clunky and slow.
Also, data integrations are fragile by nature. 
Existing products do not provide a testable way to build integrations and detect when they stop functioning.

A developer would often prefer fine-grained, programmatic access to the data they are querying, transforming, and persisting.
A developer would also like to be able to test their integration.

__IntegrationEngine__ provides this.

##How does it work?
 
1. __IntegrationEngine__ is a library. In order to use the library a developer must create a .NET console or service project.
When the engine is initialized, in a host application, integration jobs that implement 
_IntegrationEngine.Model.IIntegrationJob_ are scheduled. 
The assembly the integration jobs are located in must be provided to the engine when the engine is instantiated.
1. Integration jobs are scheduled by posting a request to the __IntegrationEngine__ web API, 
by default located at http://localhost:9001.
1. When a job is triggered a message is added to the RabbitMQ message queue defined in [IntegrationEngine.json](configuration.html) that indicates which job to run.
1. When a message is detected, a RabbitMQ message listener plucks the message from the queue and runs the job encoded within it.

##How is this software licensed?
[MIT](https://github.com/ethanhann/IntegrationEngine/blob/master/LICENSE)
