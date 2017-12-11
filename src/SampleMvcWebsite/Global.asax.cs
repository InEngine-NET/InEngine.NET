using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using InEngine.Core.Scheduling;

namespace SampleMvcWebsite
{
    public class Global : HttpApplication
    {
        public SuperScheduler SuperScheduler { get; set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            SuperScheduler = new SuperScheduler();
            SuperScheduler.Initialize();
            SuperScheduler.Start();
        }

        protected void Application_End()
        {
            SuperScheduler?.Shutdown();
        }
    }
}
