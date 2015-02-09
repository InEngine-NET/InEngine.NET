using System;

namespace IntegrationEngine.Core.Configuration
{
    public interface IMailConfiguration : IIntegrationPointConfiguration
    {
        string HostName { get; set; }
        int Port { get; set; }
    }
}

