using System.Linq;

namespace IntegrationEngine.Core.Configuration
{
    public class ElasticsearchConfiguration : IElasticsearchConfiguration
    {
        public string IntegrationPointName { get; set; }
        public string Protocol { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public string DefaultIndex { get; set; }

        public ElasticsearchConfiguration()
        {
        }

        public ElasticsearchConfiguration(IEngineConfiguration engineConfiguration, string integrationPointName)
            : this()
        {
            var config = engineConfiguration.IntegrationPoints.Elasticsearch.Single(x => x.IntegrationPointName == integrationPointName);
            IntegrationPointName = integrationPointName;
            Protocol = config.Protocol;
            HostName = config.HostName;
            Port = config.Port;
            DefaultIndex = config.DefaultIndex;
        }
    }
}
