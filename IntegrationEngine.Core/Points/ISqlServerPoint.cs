using System;

namespace IntegrationEngine.Core.Points
{
    public interface ISqlServerPoint : IIntegrationPoint
    {
        string HostName { get; set; }
        uint Port { get; set; }
        string DatabaseName { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
    }
}

