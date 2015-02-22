---
layout: default
currentMenu: integration-jobs
---

# Integration Jobs

Integration jobs contain a _Run_ method, thanks to _IIntegrationJob_, that run when the job is executed by the __InEngine.NET__ scheduler.

Integration jobs utilize [Integration Points](integration-points.html) to query and persist data. 
They can also send an email, run a console command, call some third party library, or anything else that can 
be done in a .NET application.

## Built-in Job Types

__InEngine.NET__ comes with a few built-in integration jobs, defined in _IntegrationEngine.Core.IntegrationJob_, described below.

### Integration Job

* Implement the _IIntegrationJob_ interface to create a basic integration job.
* An integration job is a C# class that implements the _IIntegrationJob_ interface.
* The interface has one argumentless method called _Run_ which is executed when the job is triggered.
* All other jobs must implement _IIntegrationJob_ directly or indirectly.

### Parameterized Job

* Implement the _IParameterizedJob_ interface to create a parameterized job.
* There is no need to directly implement _IIntegrationJob_ when implementing _IParameterizedJob_ as it already implements _IIntegrationJob_.  
* The _Parameters_ property of _IParameterizedJo db_ is automatically instantiated and populated with the _Parameters_ IDictionary stored in the trigger that is used to schedule the job.
* _Parameters_ can be used to configure the job in some way.
