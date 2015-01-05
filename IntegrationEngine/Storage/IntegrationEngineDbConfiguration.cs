using MySql.Data.Entity;
using MySql.Data.MySqlClient;
using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using System.Reflection;

namespace IntegrationEngine.Storage
{
    public class IntegrationEngineDbConfiguration : DbConfiguration
    {
        public IntegrationEngineDbConfiguration()
            : this((new EngineJsonConfiguration()).DatabaseConfiguration.ServerType)
        {
        }

        public IntegrationEngineDbConfiguration(string serverType)
        {
            if (serverType == "MySQL")
            {
                SetExecutionStrategy(MySqlProviderInvariantName.ProviderName, () => new MySqlExecutionStrategy());
                SetProviderFactory(MySqlProviderInvariantName.ProviderName, new MySqlClientFactory()); 
                SetProviderServices(MySqlProviderInvariantName.ProviderName, new MySqlProviderServices());
                SetDefaultConnectionFactory(new MySqlConnectionFactory());
            }
            else if (serverType == "SQLite")
            {
                SetDefaultConnectionFactory(new SqliteConnectionFactory());
                SetProviderFactory("System.Data.SQLite", SQLiteFactory.Instance);
                SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
                Type t = Type.GetType("System.Data.SQLite.EF6.SQLiteProviderServices, System.Data.SQLite.EF6");
                FieldInfo fi = t.GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static);
                SetProviderServices("System.Data.SQLite", (DbProviderServices)fi.GetValue(null));
            }
            else if (serverType == "SQLServer")
            {
                SetExecutionStrategy("System.Data.SqlClient", () => new SqlAzureExecutionStrategy());
                SetDefaultConnectionFactory(new LocalDbConnectionFactory("v11.0"));
            }
        }
    }
}
