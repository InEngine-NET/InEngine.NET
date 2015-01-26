---
layout: default
currentMenu: integration-jobs
---

# Integration Jobs

Integration jobs can do anything, not just retrieve, transform, and persist data.
They can also be made to send an email, run a console command, call some third party library, or anything else that can 
be done in a .NET application.

## Built-in Job Types

__InEngine.NET__ comes with a few built-in integration jobs defined in _IntegrationEngine.Core.Jobs_.
They are __Integration Job__, __Mail Job__, __Elasticsearch Job__, __Log Job__, and __SQL Job__.

### Integration Job

* Implement the _IIntegrationJob_ interface to create a basic integration job.
* An integration job is a C# class that implements the _IIntegrationJob_ interface.
* The interface has one argumentless method called _Run_ which is executed when the job is triggered.
* All other jobs must implement _IIntegrationJob_ directly or indirectly.

### Elasticsearch Job

* Implement the _IElasticsearchJob_ interface to create an Elasticsearch Job.
* The _ElasticClient_ property of the _IElasticsearchJob_ is automatically instantiated and configured with the Elasticsearch settings in [IntegrationEngine.json](configuration.html).
* The _ElasticClient_ is an instance of _Nest.ElasticClient_. 
* See the NEST [quick start](http://nest.azurewebsites.net/nest/quick-start.html) for information on how to use it.

### Log Job

* Implement the _ILogJob_ interface to create a log job.
* The _Log_ property of the _ILogJob_ is automatically instantiated and configured with the 
[log4net](http://logging.apache.org/log4net/) settings in the host applications App.config file.
* The _Log_ instance can be configured to log to a variety of [logging targets](logging.apache.org/log4net/release/features.html#appenders).
* Messages and exception can be logged according to their [severity](http://logging.apache.org/log4net/release/manual/introduction.html#hierarchy).

### Mail Job

* Implement the _IMailJob_ interface to create a mail job.
* The _MailClient_ property of the _IMailJob_ is automatically instantiated and configured with the mail settings in [IntegrationEngine.json](configuration.html).
* The _MailClient_'s _Send_ method can be used to send a _System.Net.Mail.MailMessage_.

### Parameterized Job

* Implement the _IParameterizedJob_ interface to create a parameterized job.
* The _Parameters_ property of the _IParameterizedJob_ is automatically instantiated and populated with the Parameters dictionary stored in the trigger that is used to schedule the job.
* _Parameters_ can be used to configure the job in some way.

### SQL Job

* Implement the _ISqlJob_ interface to create a sql job.
* The _DbContext_ property of the _ISqlJob_ is automatically instantiated and configured with the Database settings in [IntegrationEngine.json](configuration.html).
* The _Query_ property of the _ISqlJob_ must be populated with a valid SQL command before the _RunQuery<T>_ method is called.
* The generic parameter in the _RunQuery<T>_ method is a class that represents a record returned from the database.
* The _RunQuery<T>_ method returns a _IList<T>_ collection of records from the database.

#### Concrete SQL Job Class

There is also concrete class defined in _IntegrationEngine.Core.Jobs_ called _SqlJob_. 

* It implements _ISqlJob_, _IMailJob_, amd _ILogJob_.
* A class derived from _SqlJob_ will run whatever query is assigned to its _Query_ property by default.
* However, both the _RunQuery<T>_ and _Run_ methods are marked as virtual and thus can be overridden.
* A common practice is to override the _Run_ method, call _RunQuery<T>_, examine the results, then send a notification email.
* The _Log_ property can be used to log when the query fails.
