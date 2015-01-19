---
layout: default
currentMenu: home
---

# Welcome

__InEngine.NET__ is a set of .NET packages, created by [Ethan Hann](http://ethanhann.com), that allows for the 
creation of a code-centric data integration and asynchronous job scheduling server.

- [NuGet Package](https://www.nuget.org/packages/IntegrationEngine)
- [GitHub Project](https://github.com/ethanhann/InEngine.NET)

### Why should you care?

Data integration is a common task when working in a large or medium enterprise environment.
Data integration servers take data from a data source, transform it (or not), and put it somewhere. 
__InEngine.NET__ can allow you to do that and more in a programmatic manner.

There are several existing full-featured data integration server products on the market.
These products do not work in a way software developers are comfortable with.
They provide a drag-and-drop GUI application for implementing a data integration.
This sounds appealing on the surface, but such GUIs are often clunky and slow.
Additionally, existing products do not provide a testable way to build integrations and detect when they stop functioning.
This is a major issue as data integrations are fragile by nature.

In contrast, a developer would prefer fine-grained, programmatic access to the data they are querying, transforming, and persisting.
A developer would also like to be able to test their integrations to ensure their continued operation. 
__InEngine.NET__ provides this.

### How does it work?
 
1. __InEngine.NET__ is a library. In order to use the library a developer must create a .NET console or service 
project that instantiates and initializes an instance of _IntegrationEngine.EngineHost_. 
1. Integration jobs that implement _IntegrationEngine.Model.IIntegrationJob_ (located in an assembly passed to _EngineHost_) are loaded.
1. Integration jobs are scheduled by posting a request to the [InEngine.NET Web API](web-api.html).
1. When a job is triggered a message is added to the message queue, defined in [IntegrationEngine.json](configuration.html), that indicates which job to run.
1. When a message is detected, the __InEngine.NET__ job runner plucks the message from the queue and runs the job encoded within it.

<img src="https://docs.google.com/drawings/d/1dEmGlhfDWhljOjWIn7ttuNQxfY1N_dXOaHxNOLpgV9U/pub?w=960&amp;h=720" alt="IntegrationEngine Job Processing Diagram" />

### How is this software licensed?
[MIT](https://github.com/ethanhann/InEngine.NET/blob/master/LICENSE)
