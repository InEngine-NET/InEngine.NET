---
layout: default
currentMenu: change-log
---

# Change Log

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
