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
    }
}

