using System;

namespace IntegrationEngine.Core.Configuration
{
    public interface IJsonServiceConfiguration : IIntegrationPointConfiguration
    {
        string BaseUri { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
        bool IgnoreInvalidSslCertificate { get; set; }
    }
}

