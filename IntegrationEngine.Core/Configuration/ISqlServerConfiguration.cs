using System;

namespace IntegrationEngine.Core.Configuration
{
    public interface ISqlServerConfiguration : IIntegrationPointConfiguration
    {
        string HostName { get; set; }
        uint Port { get; set; }
        string DatabaseName { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
    }
}

