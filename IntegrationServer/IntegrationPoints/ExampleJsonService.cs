using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.ServiceStack;

namespace IntegrationServer.IntegrationPoints
{
    public class ExampleJsonService : JsonServiceClientAdapter
    {
        public ExampleJsonService(IJsonServiceConfiguration jsonServiceConfiguration)
        {
            JsonServiceConfiguration = jsonServiceConfiguration;
        }
    }
}
