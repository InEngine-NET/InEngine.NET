---
layout: default
currentMenu: getting-started
---

#Getting Started

## Required Software

* [RabbitMQ](http://www.rabbitmq.com/download.html)
* [Elasticsearch](http://www.elasticsearch.org/overview/elkdownloads/)

##Install

To install IntegrationEngine, run the following command in the Package Manager Console.
```
PM> Install-Package IntegrationEngine
```

##Initialize
__IntegrationEngine__ requires a host application. 
It can be a console or service application.
The following code snippet demonstrates how to instantiate and initialize __IntegrationEngine__ in a console app called _MyProject_.
It also indicates to the instance of _EngineHost_ that it should look in the the assembly containing class _MainClass_
for integration jobs.

```
// Program.cs
using System;
using System.Reflection;

namespace MyProject
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            (new EngineHost(typeof(MainClass).Assembly)).Initialize();
        }
    }
}
```

## Create an Integration Job

```
// MyIntegrationJob.cs
using IntegrationEngine.Core.Jobs;

namespace MyProject
{
    public class MyIntegrationJob : IIntegrationJob
    {
        public override void Run()
        {
            // Do some work
        }
    }
}
```

## Schedule a Job
Post an HTTP request to the IntegrationServer API's IntegrationJob resource with a _JobType_ of 
"MyProject.MyIntegrationJob", the full name of the job.  

```
curl --data "JobType=MyProject.MyIntegrationJob" http://localhost:9001/api/integrationjob
```
