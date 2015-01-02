using Microsoft.Owin.Hosting;

namespace IntegrationEngine.Api
{
    class IntegrationEngineApi
    {
        static IntegrationEngineApi()
        {}

        public static void Start(string baseAddress)
        {
            WebApp.Start<Startup>(url: baseAddress);
        }
    }
}
