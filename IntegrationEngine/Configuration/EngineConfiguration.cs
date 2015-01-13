using FX.Configuration;
using IntegrationEngine.Core.Configuration;

namespace IntegrationEngine.Configuration
{
    public class EngineConfiguration : JsonConfiguration
    {
        public EngineConfiguration() : base("IntegrationEngine.json")
        {}

        public WebApiConfiguration WebApi { get; set; }
        public MessageQueueConfiguration MessageQueue { get; set; }
        public MailConfiguration Mail { get; set; }
        public DatabaseConfiguration Database { get; set; }
        public ElasticsearchConfiguration Elasticsearch { get; set; }
    }
}
