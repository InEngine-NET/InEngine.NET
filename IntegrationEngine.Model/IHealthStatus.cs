using System;

namespace IntegrationEngine.Model
{
    public interface IHealthStatus
    {
        bool IsMailServerAvailable { get; set; }
        bool IsMessageQueueServerAvailable { get; set; }
        bool IsElasticsearchServerAvailable { get; set; }
    }
}

