using System;

namespace IntegrationEngine.Core.Points
{
    public interface IElasticsearchPoint
    {
        string Protocol { get; set; }
        string HostName { get; set; }
        int Port { get; set; }
        string DefaultIndex { get; set; }
    }
}
