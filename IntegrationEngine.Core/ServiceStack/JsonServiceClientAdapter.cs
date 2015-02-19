using ServiceStack;
using IntegrationEngine.Core.Configuration;

namespace IntegrationEngine.Core.ServiceStack
{
    public class JsonServiceClientAdapter : JsonServiceClient, IJsonServiceClient
    {
        public JsonServiceClientAdapter(IJsonServiceConfiguration jsonServiceConfiguration) 
            : base(jsonServiceConfiguration.BaseUri)
        {
            UserName = jsonServiceConfiguration.UserName;
            Password = jsonServiceConfiguration.Password;
        }
    }
}
