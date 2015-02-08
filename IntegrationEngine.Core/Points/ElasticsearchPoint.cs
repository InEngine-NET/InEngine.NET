using System;

namespace IntegrationEngine.Core.Points
{
    public class ElasticsearchPoint : IElasticsearchPoint
    {
        public string Protocol { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public string DefaultIndex { get; set; }

        public ElasticsearchPoint()
        {
        }
    }
}

