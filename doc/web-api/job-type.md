---
layout: web-api
currentMenu: web-api
currentSubMenu: job-type
---

## JobType Endpoint

A list of registered Job types.

### Parameters

<div class="table-responsive">
<table class="table table-bordered">
<thead><tr><th>Param</th><th>Type</th><th>Details</th></tr></thead>
<tbody>
    <tr>
        <td>Name</td>
        <td><span class="label label-info">string</span></td></td>
        <td>The name of the integration job class.</td>
    </tr>
    <tr>
        <td>FullName</td>
        <td><span class="label label-info">string</span></td></td>
        <td>The fully qualified name (namespace + class) of the integration job class.</td>
    </tr>
</tbody>
</table>
</div>

### Get a List of JobTypes
GET api/JobType
```sh   
curl http://localhost:9001/api/JobType
```
