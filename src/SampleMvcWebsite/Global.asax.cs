using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using InEngine.Core;

namespace SampleMvcWebsite
{
    public class Global : HttpApplication
    {
        public ServerHost ServerHost { get; set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            ServerHost = new ServerHost();
            ServerHost.Start();
        }

        protected void Application_End()
        {
            ServerHost.Dispose();
        }
    }
}
