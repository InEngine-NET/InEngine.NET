
namespace IntegrationEngine.Core.Configuration
{
    public class ElasticsearchConfiguration : IElasticsearchConfiguration
    {
        public string IntegrationPointName { get; set; }
        public string Protocol { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public string DefaultIndex { get; set; }
    }
}
