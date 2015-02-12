using IntegrationEngine.Core.Configuration;
using System.Collections.Generic;

namespace IntegrationEngine.Core.Configuration
{
    public interface IEngineConfiguration
    {
        WebApiConfiguration WebApi { get; set; }
        NLogAdapterConfiguration NLogAdapter { get; set; }
        IntegrationPointConfigurations IntegrationPoints { get; set; }
    }
}
