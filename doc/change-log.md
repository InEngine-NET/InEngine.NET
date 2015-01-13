---
layout: default
currentMenu: change-log
---

# Change Log

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
    * Load jobs from an external assembly that implement IIntegrationJob.
    * Develop EngineHost, start engine and load jobs from an external assembly.
    * Add repository layer for persisting jobs to a data store.
    * Create REST API for creating standard jobs.
* Chores
    * Publish packages on NuGet.
