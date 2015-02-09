using System;

namespace IntegrationEngine.Core.Configuration
{
    public interface IElasticsearchConfiguration
    {
        string Protocol { get; set; }
        string HostName { get; set; }
        int Port { get; set; }
        string DefaultIndex { get; set; }
    }
}
