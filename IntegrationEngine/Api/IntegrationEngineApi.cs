using Microsoft.Owin.Hosting;
using Microsoft.Owin.Host.HttpListener;

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
