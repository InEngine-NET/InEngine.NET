---
layout: web-api
currentMenu: web-api
currentSubMenu: log-event
---

## LogEvent Endpoint

The LogEvent endpoint allows for the retrieval of log events.

### Parameters

<div class="table-responsive">
<table class="table table-bordered">
<thead><tr><th>Param</th><th>Type</th><th>Details</th></tr></thead>
<tbody>
    <tr>
        <td>Id</td>
        <td><span class="label label-info">string</span></td></td>
        <td>A unique, auto-generated identifier for the log event.</td>
    </tr>
    <tr>
        <td>StartTimeUtc</td><td><a href="http://msdn.microsoft.com/en-us/library/system.datetimeoffset%28v=vs.110%29.aspx">System.DateTimeOffset</a></td>
        <td>The time at which the log event was created.</td>
    </tr>
    <tr>
        <td>Level</td>
        <td><span class="label label-info">string</span></td></td>
        <td>The <a href="http://en.wikipedia.org/wiki/Log4j#Log4j_1_Log_level">level</a> of the log event.</td>
    </tr>
    <tr>
        <td>Message</td>
        <td><span class="label label-info">string</span></td></td>
        <td>The textual message body of the log event.</td>
    </tr>
</tbody>
</table>
</div>
### Get a List of TimeZones
GET api/LogEvent
```sh   
curl http://localhost:9001/api/LogEvent
```
