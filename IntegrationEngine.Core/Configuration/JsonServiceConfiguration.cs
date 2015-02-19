using System.Linq;

namespace IntegrationEngine.Core.Configuration
{
    public class JsonServiceConfiguration : IJsonServiceConfiguration
    {
        public string IntegrationPointName { get; set; }
        public string BaseUri { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool AlwaysSendBasicAuthHeader { get; set; }

        public JsonServiceConfiguration()
        {
        }

        public JsonServiceConfiguration(IEngineConfiguration engineConfiguration, string integrationPointName)
            : this()
        {
            var config = engineConfiguration.IntegrationPoints.JsonService.Single(x => x.IntegrationPointName == integrationPointName);
            IntegrationPointName = integrationPointName;
            BaseUri = config.BaseUri;
            UserName = config.UserName;
            Password = config.Password;
            AlwaysSendBasicAuthHeader = config.AlwaysSendBasicAuthHeader;
        }
    }
}

