---
layout: default
currentMenu: getting-started
---

# Getting Started

## Install Required Software

* [RabbitMQ](http://www.rabbitmq.com/download.html)
* [Elasticsearch](http://www.elasticsearch.org/overview/elkdownloads/)

## Create a Console Application

[Create a console application](http://msdn.microsoft.com/en-us/library/k1sx6ed2.aspx) called "MyProject" in Visual Studio or Xamarin Studio. 

## Add the InEngine.NET Package 

### Visual Studio
Run the following command in the Package Manager Console.
```sh
PM> Install-Package IntegrationEngine
```

### Xamarin Studio
Navigate to _Project_&#8594;_Add Packages_, then search for "InEngine.NET" in the _Add Packages_ GUI.

## Initialize InEngine.NET
__InEngine.NET__ requires a host application. 
It can be a console or service application.
The following code snippet demonstrates how to instantiate and initialize __InEngine.NET__ in a console app called _MyProject_.
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

## Add Configuration File
The [configuration](configuration.html) file should be called "IntegrationEngine.json" and its build action should be to "copy if newer."

## Create an Integration Job

```
// MyIntegrationJob.cs
using IntegrationEngine.Core.IntegrationJob;

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
Post an HTTP request to the IntegrationServer API's CronTrigger resource with a _JobType_ of
"MyProject.MyIntegrationJob" - the full name of the job.  

```sh
curl --data "JobType=MyProject.MyIntegrationJob&CronExpressionString=0 4 1 ? * MON-FRI *" http://localhost:9001/api/CronTrigger
```
