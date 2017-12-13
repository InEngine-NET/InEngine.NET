# Server

When run as a service, InEngine runs scheduled commands in the background and actively listens for commands to be queued.

## Running the Server

The server can be run in a variety of ways.

### In the Foreground

Running the server from the CommandLine is useful for debugging or local development:

```bash
inengine.exe -s
```

It can also be run on Mac and Linux with Mono via a shell wrapper script:

```bash
./inengine -s
``` 

### In ASP.NET 

The server can be run in Global.asax.cs:

```c#
using System.Web;
using InEngine.Core;

namespace MyWebApp
{
    public class Global : HttpApplication
    {
        public ServerHost ServerHost { get; set; }

        protected void Application_Start()
        {
            ServerHost = new ServerHost();
            ServerHost.Start();
        }

        protected void Application_End()
        {
            ServerHost.Dispose();
        }
    }
}
``` 

### On Windows as a Service

Run the **Install.ps1** PowerShell script in the InEngine directory to install the InEngine as a service. 
The script needs to be run as an administrator. 
The script will register the service at the location where the script is run - i.e. put the files where you want them installed before running the installation script.

```bash
ps Install.ps1
```

Simply run the **Uninstall.ps1** script with elevated permissions to remove the service.

```bash
ps Uninstall.ps1
```

### On Linux with Supervisor

Supervisor is a process control system for Linux. 
It has extensive [documentation](http://supervisord.org/index.html), but the following should be enough to get started.

#### Installing Supervisor

This command installs Supervisor on Ubuntu:

```bash
sudo apt-get install supervisor
```

#### Configuring Supervisor

Supervisor configuration files are stored in the **/etc/supervisor/conf.d** directory. Multiple files can be created in this directory to specify different programs, or multiple instances of the same program, for Supervisor to monitor. Copy this sample config into a file called **/etc/supervisor/conf.d/inengine-scheduler.conf**. 

```ini
[program:inengine]
process_name=%(program_name)s_%(process_num)02d
directory=/path/to/inengine
command=mono inengine.exe -s
autostart=true
autorestart=true
user=InEngine
numprocs=1
redirect_stderr=true
stdout_logfile=./inengine.log
```

#### Starting Supervisor

Whenever a configuration change happens to files in the Supervisor config files, Supervisor needs to be instructed to reload its configuration.

```bash
sudo supervisorctl reread
sudo supervisorctl update
```

Now, simply start the server workers with the **supervisorctl** program:

```bash
sudo supervisorctl start inengine:*
```

### In a Container with Docker

Install [Docker](https://www.docker.com/what-docker) first, then pull the **ethanhann/inengine** image:

```bash
docker pull ethanhann/inengine:latest
```

Now run the InEngine in server mode:

```bash
docker run --rm ethanhann/inengine -s
```
