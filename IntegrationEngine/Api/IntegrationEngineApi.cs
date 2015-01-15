using System.Web.Http;
using Microsoft.Owin.Host.HttpListener;
using Microsoft.Owin.Hosting;

namespace IntegrationEngine.Api
{
    class IntegrationEngineApi
    {
        static IntegrationEngineApi()
        {}

        public static void Start(string baseAddress)
        {
            WebApp.Start<Startup>(baseAddress);
        }
    }
}
