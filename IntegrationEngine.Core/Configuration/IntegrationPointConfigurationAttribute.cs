using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Configuration
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class IntegrationPointConfigurationByAttribute : Attribute
    {
        public IIntegrationPointConfiguration IntegrationPointConfiguration { get; set; }

        public IntegrationPointConfigurationByAttribute(IIntegrationPointConfiguration integrationPointConfiguration)
        {
            IntegrationPointConfiguration = integrationPointConfiguration;
        }
    }
}
