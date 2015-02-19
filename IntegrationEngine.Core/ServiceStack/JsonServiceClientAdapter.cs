using IntegrationEngine.Core.Configuration;
using ServiceStack.ServiceClient.Web;
using System.Net;

namespace IntegrationEngine.Core.ServiceStack
{
    public class JsonServiceClientAdapter : JsonServiceClient, IJsonServiceClient
    {
        public IJsonServiceConfiguration JsonServiceConfiguration { get; set; }

        public JsonServiceClientAdapter()
        { 
        }

        public JsonServiceClientAdapter(IJsonServiceConfiguration jsonServiceConfiguration) 
            : base(jsonServiceConfiguration.BaseUri)
        {
            UserName = jsonServiceConfiguration.UserName;
            Password = jsonServiceConfiguration.Password;
            AlwaysSendBasicAuthHeader = jsonServiceConfiguration.AlwaysSendBasicAuthHeader;
            JsonServiceConfiguration = jsonServiceConfiguration;
        }
    }
}
