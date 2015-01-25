---
layout: web-api
currentMenu: web-api
currentSubMenu: simple-trigger
---

## SimpleTrigger Endpoint

The SimpleTrigger endpoint allows for an integration job to be scheduled by creating and manipulating a Quartz.NET SimpleTrigger.

### Parameters

<table class="table table-bordered">
<thead><tr><th>Param</th><th>Type</th><th>Details</th></tr></thead>
<tbody>
    <tr><td>Id</td><td><span class="label label-info">string</span></td></td>
        <td>A unique, auto-generated identifier for the trigger.</td>
    </tr>
    <tr><td>JobType</td><td><span class="label label-info">string</span></td></td>
        <td>The <a href="http://msdn.microsoft.com/en-us/library/system.type.fullname%28v=vs.110%29.aspx">FullName</a> of an <a href="integration-jobs.html">Integration Job</a> type.</td>
    </tr>
    <tr><td>StateId</td><td><span class="label label-info">int</span></td>
        <td>An integer identifier that sets the state of the trigger. Valid values are 0 (active) and 1 (paused).</td>    
    </tr>
    <tr>
        <td>Parameters</td>
        <td><a href="https://msdn.microsoft.com/en-us/library/s4ys34ea%28v=vs.110%29.aspx">System.Collections.Generic.IDictionary<string,string></a></td>
        <td>A key/value object that is made available to integration jobs that implement the _IParameterizedJob_ interface.</td>
    </tr>
    <tr><td>RepeatInterval</td><td><a href="http://msdn.microsoft.com/en-us/library/system.timespan%28v=vs.110%29.aspx">System.TimeSpan</a></td>
        <td>Time interval at which the trigger should repeat.</td>
    </tr>
    <tr><td>RepeatCount</td><td><span class="label label-danger">int</span></td>
        <td>The number of times the trigger should repeat, after which it will be automatically deleted.</td>
    </tr>
    <tr><td>StartTimeUtc</td><td><a href="http://msdn.microsoft.com/en-us/library/system.datetimeoffset%28v=vs.110%29.aspx">System.DateTimeOffset</a></td>
        <td>The time at which the trigger's scheduling should start. 
        If the value is the default value of System.DateTimeOffset, then the trigger fires when the scheduler starts.</td>
    </tr>
</tbody>
</table>

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
curl --data "JobType=MyProject.MyIntegrationJob&RepeatInterval=00:00:05&RepeateCount=1" http://localhost:9001/api/SimpleTrigger
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
