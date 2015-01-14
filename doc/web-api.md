---
layout: default
currentMenu: web-api
---

# Web API

<h2>CronTrigger Endpoint <small>&ge;2.0.0</small></h2>

The CronTrigger endpoint allows for an integration job to be scheduled by creating and manipulating a Quartz.NET CronTrigger.

### Get a List of CronTriggers
GET api/CronTrigger
```sh   
curl http://localhost:9001/api/CronTrigger
```

### Get a Specific CronTrigger by ID
GET api/CronTrigger/ID
```sh
curl http://localhost:9001/api/CronTrigger/ID
```

### Create a New CronTrigger
POST api/CronTrigger
```sh
curl --data "JobType=MyProject.MyIntegrationJob" http://localhost:9001/api/CronTrigger
```

### Update a Specific CronTrigger by ID
PUT api/CronTrigger/ID
```sh
curl -XPUT "JobType=MyProject.ADifferentIntegrationJob" http://localhost:9001/api/CronTrigger/ID
```

### Delete a CronTrigger by ID
DELETE api/CronTrigger/ID
```sh
curl -XDELETE http://localhost:9001/api/CronTrigger/ID
```

<h2>SimpleTrigger Endpoint <small>&ge;2.0.0</small></h2>

The SimpleTrigger endpoint allows for an integration job to be scheduled by creating and manipulating a Quartz.NET SimpleTrigger.

### Get a List of SimpleTriggers
GET api/SimpleTrigger
```sh   
curl http://localhost:9001/api/SimpleTrigger
```

### Get a Specific SimpleTrigger by ID
GET api/SimpleTrigger/ID
```sh
curl http://localhost:9001/api/SimpleTrigger/ID
```

### Create a New SimpleTrigger
POST api/SimpleTrigger
```sh
curl --data "JobType=MyProject.MyIntegrationJob" http://localhost:9001/api/SimpleTrigger
```

### Update a Specific SimpleTrigger by ID
PUT api/SimpleTrigger/ID
```sh
curl -XPUT "JobType=MyProject.ADifferentIntegrationJob" http://localhost:9001/api/SimpleTrigger/ID
```

### Delete a SimpleTrigger by ID
DELETE api/SimpleTrigger/ID
```sh
curl -XDELETE http://localhost:9001/api/SimpleTrigger/ID
```

<h2>IntegrationJob Endpoint <small>&le;1.4.1</small></h2>

The IntegrationJob endpoint allows for an integration job to be scheduled, updated, and viewed.

### Get a List of Jobs
GET api/IntegrationJob
```sh
curl http://localhost:9001/api/IntegrationJob
```

### Get a Specific Job by ID
GET api/IntegrationJob/ID
```sh
curl http://localhost:9001/api/IntegrationJob/ID
```

### Create a New Job
POST api/IntegrationJob
```sh
curl --data "JobType=MyProject.MyIntegrationJob" http://localhost:9001/api/IntegrationJob
```

### Update a Specific Job by ID
PUT api/IntegrationJob/ID
```sh
curl -XPUT "JobType=MyProject.ADifferentIntegrationJob" http://localhost:9001/api/IntegrationJob/ID
```

### Delete a Job by ID
DELETE api/IntegrationJob/ID
```sh
curl -XDELETE http://localhost:9001/api/IntegrationJob/ID
```