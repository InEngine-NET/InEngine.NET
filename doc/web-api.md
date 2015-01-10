---
layout: default
currentMenu: web-api
---

# Web API

## IntegrationJob Endpoint

The IntegrationJob endpoint allows for an integration job to be scheduled, updated, and viewed.

### Get a List of Jobs
GET api/IntegrationJob
```
curl http://localhost:9001/api/IntegrationJob
```

### Get a Specific Job by ID
GET api/IntegrationJob/ID
```
curl http://localhost:9001/api/IntegrationJob/ID
```

### Create a New Job
POST api/IntegrationJob
```
curl --data "JobType=MyProject.MyIntegrationJob" http://localhost:9001/api/IntegrationJob
```

### Update a Specific Job by ID
PUT api/IntegrationJob/ID
```
curl -XPUT "JobType=MyProject.ADifferentIntegrationJob" http://localhost:9001/api/IntegrationJob/ID
```

### Delete a Job by ID
DELETE api/IntegrationJob/ID
```
curl -XDELETE http://localhost:9001/api/IntegrationJob/ID
```