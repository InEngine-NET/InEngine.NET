---
layout: default
currentMenu: dashboard
---

# Web Dashboard

The dashboard is a web front-end, built with AngularJS, for the InEngine.NET Web API.
It uses the [eeh-inengine-api](angularjs-api-module.html) AngularJS API Module to access the API.

The dashboard source is available on [GitHub](https://github.com/ethanhann/InEngine.NET-Dashboard).

## Configuration

Be sure to add the domain the dashboard is served from to the WebAPI/Origins section of the [IntegrationEngine.json file](configuration.html).
 
## Serving the Dashboard
 
The dashboard is not served from the web API.
It is a Node.js application.
To build and serve the dashboard, first download and install [Node.js](http://nodejs.org/),
then navigate to the root of the project use NPM to install dependencies.

```sh
npm install
```

Now, use NPM to serve the dashboard.

```sh
npm start
```

The dashboard will be served at _http://localhost:3000_.
 
Though the dashboard is served by Node.js, any web server can serve it since the dashboard is merely a collection of static files. 
IIS, Apache HTTP Server, and Nginx are all options.
