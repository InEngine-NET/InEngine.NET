using IntegrationEngine.Core.Configuration;
using MySql.Data.Entity;
using MySql.Data.MySqlClient;
using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;
using System.Reflection;

namespace IntegrationEngine.Core.Storage
{
    public class IntegrationEngineDbConfiguration : DbConfiguration
    {
        public IntegrationEngineDbConfiguration()
            : base()
        {}

        public IntegrationEngineDbConfiguration(string serverType)
        {
            if (serverType == "MySQL")
            {
                SetExecutionStrategy(MySqlProviderInvariantName.ProviderName, () => new MySqlExecutionStrategy());
                SetProviderFactory(MySqlProviderInvariantName.ProviderName, new MySqlClientFactory()); 
                SetProviderServices(MySqlProviderInvariantName.ProviderName, new MySqlProviderServices());
                SetDefaultConnectionFactory(new MySqlConnectionFactory());
            }
            else if (serverType == "SQLServer")
            {
                SetExecutionStrategy("System.Data.SqlClient", () => new SqlAzureExecutionStrategy());
                SetDefaultConnectionFactory(new LocalDbConnectionFactory("v11.0"));
            }
        }
    }
}
