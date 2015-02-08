using FX.Configuration;
using IntegrationEngine.Core.Configuration;

namespace IntegrationEngine.Configuration
{
    public class EngineConfiguration : JsonConfiguration
    {
        public EngineConfiguration() : base("IntegrationEngine.json")
        {}

        public WebApiConfiguration WebApi { get; set; }
        public RabbitMQConfiguration MessageQueue { get; set; }
        public ElasticsearchConfiguration Elasticsearch { get; set; }
        public NLogAdapterConfiguration NLogAdapter { get; set; }
    }
}
