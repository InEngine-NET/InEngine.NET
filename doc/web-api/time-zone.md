---
layout: web-api
currentMenu: web-api
currentSubMenu: time-zone
---

## TimeZone Endpoint

The TimeZone endpoint allows for the retrieval of a system's TimeZone list, provided internally by the [TimeZoneInfo.GetSystemTimeZones](http://msdn.microsoft.com/en-us/library/system.timezoneinfo.getsystemtimezones%28v=vs.110%29.aspx) method.

### Parameters

<table class="table table-bordered">
<thead><tr><th>Param</th><th>Type</th><th>Details</th></tr></thead>
<tbody>
    <tr><td>Id</td><td><span class="label label-info">string</span></td></td>
        <td>The time zone identifier. A suitable value for a CronTrigger's TimeZoneId field.</td>
    </tr>
    <tr><td>DisplayName</td><td><span class="label label-info">string</span></td></td>
        <td>The general display name that represents the time zone.</td>
    </tr>
</tbody>
</table>

### Get a List of TimeZones
GET api/TimeZone
```sh   
curl http://localhost:9001/api/TimeZone
```
