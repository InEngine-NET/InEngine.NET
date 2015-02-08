using System;

namespace IntegrationEngine.Core.Points
{
    public interface IMailPoint : IIntegrationPoint
    {
        string HostName { get; set; }
        int Port { get; set; }
    }
}

