using System;

namespace IntegrationEngine.Model
{
    public class HealthStatus : IHealthStatus
    {
        public bool IsMailServerAvailable { get; set; }
        public bool IsMessageQueueServerAvailable { get; set; }
        public bool IsElasticsearchServerAvailable { get; set; }

        public HealthStatus()
        {}

        public override string ToString()
        {
            return string.Format("[HealthStatus: IsMailServerAvailable={0}, IsMessageQueueServerAvailable={1}, IsElasticsearchServerAvailable={2}]", IsMailServerAvailable, IsMessageQueueServerAvailable, IsElasticsearchServerAvailable);
        }
    }
}

