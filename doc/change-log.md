---
layout: default
currentMenu: change-log
---

# Change Log

## 2.0.0-alpha5

* Features
    * Create Read-Only web app client called [InEngine.NET Dashboard](https://github.com/ethanhann/InEngine.NET-Dashboard).
    * Enable Cors, add "Origins" Option to Web API configuration.
    * Use Common.Logging with NLog instead of log4net.
    * Make Common.Logging NLog adapter configurable via IntegrationEngine.json.
    * Validate JobType classes as integration jobs before scheduling them.

## 2.0.0-alpha4

* Bug
    * Use correct MySql.Data.Entity dependency in Core package.

## 2.0.0-alpha3

* Chores
    * Use shared assembly to streamline package creation
    * Remove stale NuGet dependencies

## 2.0.0-alpha2

* Features
    * Return list of job keys at /api/Job.
    * Schedule a Job with a Trigger, when the Trigger is created via the Web API.
    * Reschedule a Job with a Trigger, when the Trigger is updated via the Web API.
    * Validate cron expression when creating/updating a CronTrigger.
* Chores
    * Use Unity IoC instead of Funq.

## 2.0.0-alpha1

* Features
    * Add SimpleTrigger to Model and Web API.
    * Add CronTrigger to Model and Web API.
    * Remove IntegrationJob from Model and Web API (replaced by SimpleTrigger and CronTrigger).
* Chores
    * Setup CI with AppVeyor.
* Bugs
    * Config loader no longer mistakenly looks for config.json instead of IntegrationEngine.json.

## 1.4.1

* Chores
    * Add link to change log in NuGet package release notes.

## 1.4.0

* Features
    * Make Elasticsearch configurable in IntegrationEngine.json
* Chores
    * Rename IElasticSearchJob.cs file to IElasticsearchJob.cs
    * Rename project to InEngine.NET
* Documentation
    * Getting started
    * Web API
    * Integration jobs
    * IntegrationEngine.json configuration file

## 1.3.2

* Features
    * Schedule jobs from an external assembly that implement IIntegrationJob.
    * Develop EngineHost, start engine and load jobs from an external assembly.
    * Add repository layer for persisting jobs to a data store.
    * Create REST API for creating standard jobs.
* Chores
    * Publish packages on NuGet.
