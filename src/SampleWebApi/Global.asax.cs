using System.Web;
using System.Web.Http;
using InEngine.Core;

namespace SampleWebApi
{
    public class Global : HttpApplication
    {
        public ServerHost ServerHost { get; set; }

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            ServerHost = new ServerHost();
            ServerHost.Start();
        }

        protected void Application_End()
        {
            ServerHost.Dispose();
        }
    }
}
