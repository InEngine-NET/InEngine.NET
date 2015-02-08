using IntegrationEngine.Core.Points;

namespace IntegrationEngine.Core.Configuration
{
    public class ElasticsearchConfiguration : IElasticsearchPoint
    {
        public string Protocol { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public string DefaultIndex { get; set; }
    }
}
