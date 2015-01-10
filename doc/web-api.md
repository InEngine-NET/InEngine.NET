---
layout: default
currentMenu: web-api
---

# Web API

## IntegrationJob Endpoint

### Get a List of Jobs
GET api/IntegrationJob

```
curl http://localhost:9001/api/IntegrationJob
```

### Get a Specific Job by ID
GET api/IntegrationJob/ID

```
curl http://localhost:9001/api/IntegrationJob
```

### Create a New Job
POST api/IntegrationJob

```
curl --data "JobType=MyProject.MyIntegrationJob" http://localhost:9001/api/IntegrationJob
```

### Update a Specific Job by ID
PUT api/IntegrationJob/ID

### Delete a Job by ID
DELETE api/IntegrationJob/ID