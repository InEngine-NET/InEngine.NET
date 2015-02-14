---
layout: default
currentMenu: change-log
---

# Change Log

## 2.0.0-rc2

__Chores__
* Add .net 4 version of Core assembly.
* Gracefully shutdown Web API to avoid service restart issues.
* Initialize logger in the type where it is used.

__Fixes__
* Delete SimpleTrigger from Elasticsearch repo when it is "finalized" by the scheduler. 
This prevents jobs scheduled with SimpleTriggers from possibly rerunning when the server restarts.

## 2.0.0-rc1

__Fixes__
* Use Json.NET to serialize post/put requests in IntegrationEngine.Client as RestSharp does not appear to be able to serialize a dictionary.

## 2.0.0-beta9

__Fixes__
* Return BadRequest in TriggerControllerBase instead of simply calling it.

## 2.0.0-beta8

__Features__
* Add LogEvent to Web API
* Always use UTC; remove timezone fields from model and Web API.

__Chores__
* Back fill tests for various components.
* DRYout Web API and Repository Layer.
* DRYout IntegrationEngine.Client.
* Refactor ConsoleClient internals.
* Use generic type argument for repo item Id parameters.

__Fixes__
* Resolve TCPClient issues with mail server health check.

## 2.0.0-beta7

__Features__
* Add console client that can query the web API.
* Allow parameters to be passed into a job that implement _IParameterizedJob_.
* Spawn job processing message queue listener in a thread.

__Chores__
* Add IInEngineClient interface to client project.

__Documentation__
* Add _IParameterizedJob_ info to the [Integration Jobs](integration-jobs.html) page.
* Add [Web Dashboard](dashboard.html) page.
* Add [C# Client Library](client-library.html) page.
* Add [C# Client Library](console-client.html) page.
* Add [AngularJS Client](angular-js-module.html) page.

## 2.0.0-beta6

__Features__
* Expose additional properties to typed client by pushing their implementations into model assembly.

## 2.0.0-beta5

__Features__
* Add health API for indicating server connection status.
* Remove trigger from scheduler when it is deleted via the API.

__Chores__
* Move properties from scheduler models into model interfaces.

__Fixes__
* Cron expression description not generating on Mono.

## 2.0.0-beta4

__Fixes__
* Use Json.NET to deserialize client responses.

## 2.0.0-beta3

__Features__
* Implement a typed client library.
* Add .NET 4 build of client.
* Add calendar view to dashboard.

## 2.0.0-beta2

__Chores__
* Use custom attributes for Web API validation.

__Documentation__
* Improve Web API documentation.

## 2.0.0-beta1

__Features__
* Add ability to Pause/Resume triggers.
* Switch to TimeZoneId in CronTrigger (rather than full TimeZoneInfo).
* Add read-only human readable description of CronTrigger's CronExpressionString.

## 2.0.0-alpha5

__Features__
* Create Read-Only web app client called [InEngine.NET Dashboard](https://github.com/ethanhann/InEngine.NET-Dashboard).
* Enable Cors, add "Origins" Option to Web API configuration.
* Use Common.Logging with NLog instead of log4net.
* Make Common.Logging NLog adapter configurable via IntegrationEngine.json.
* Validate JobType classes as integration jobs before scheduling them.

## 2.0.0-alpha4

__Fixes__
* Use correct MySql.Data.Entity dependency in Core package.

## 2.0.0-alpha3

__Chores__
* Use shared assembly to streamline package creation.
* Remove stale NuGet dependencies.

## 2.0.0-alpha2

__Features__
* Return list of job keys at /api/Job.
* Schedule a Job with a Trigger, when the Trigger is created via the Web API.
* Reschedule a Job with a Trigger, when the Trigger is updated via the Web API.
* Validate cron expression when creating/updating a CronTrigger.

__Chores__
* Use Unity IoC instead of Funq.

## 2.0.0-alpha1

__Features__
* Add SimpleTrigger to Model and Web API.
* Add CronTrigger to Model and Web API.
* Remove IntegrationJob from Model and Web API (replaced by SimpleTrigger and CronTrigger).

__Chores__
* Setup CI with AppVeyor.

__Fixes__
* Config loader no longer mistakenly looks for config.json instead of IntegrationEngine.json.

## 1.4.1

__Chores__
* Add link to change log in NuGet package release notes.

## 1.4.0

__Features__
* Make Elasticsearch configurable in IntegrationEngine.json.

__Chores__
* Rename IElasticSearchJob.cs file to IElasticsearchJob.cs.
* Rename project to InEngine.NET.

__Documentation__
* Getting started.
* Web API.
* Integration jobs.
* IntegrationEngine.json configuration file

## 1.3.2

__Features__
* Schedule jobs from an external assembly that implement IIntegrationJob.
* Develop EngineHost, start engine and load jobs from an external assembly.
* Add repository layer for persisting jobs to a data store.
* Create REST API for creating standard jobs.

__Chores__
* Publish packages on NuGet.
