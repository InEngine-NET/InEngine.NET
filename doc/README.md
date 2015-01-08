---
layout: default
---

#Getting Started

In general, integration servers take data from a data source, transform it (or not), and put it somewhere. 
IntegrationEngine can allow you to do that and more.

## Required Software

* [RabbitMQ](http://www.rabbitmq.com/download.html)
* [Elasticsearch](http://www.elasticsearch.org/overview/elkdownloads/)

##Install

To install IntegrationEngine, run the following command in the Package Manager Console.
```
PM> Install-Package IntegrationEngine
```

##Initialize

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
Post an HTTP request to the IntegrationServer API's IntegrationJob resource with a "JobType" of 
"MyProject.MyIntegrationJob", the full name of the job.  

```
curl --data "JobType=MyProject.MyIntegrationJob" http://localhost:9001/api/integrationjob
```
