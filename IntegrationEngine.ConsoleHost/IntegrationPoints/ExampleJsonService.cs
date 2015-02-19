using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.ServiceStack;

namespace IntegrationEngine.ConsoleHost.IntegrationPoints
{
    public class ExampleJsonService : JsonServiceClientAdapter
    {
        public ExampleJsonService(IJsonServiceConfiguration jsonServiceConfiguration)
        {
            JsonServiceConfiguration = jsonServiceConfiguration;
        }
    }
}
