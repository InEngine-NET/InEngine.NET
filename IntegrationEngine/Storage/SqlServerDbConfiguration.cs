using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;

namespace IntegrationEngine.Storage
{
    class SqlServerDbConfiguration : DbConfiguration
    {
        public SqlServerDbConfiguration() 
        { 
            SetExecutionStrategy("System.Data.SqlClient", () => new SqlAzureExecutionStrategy()); 
            SetDefaultConnectionFactory(new LocalDbConnectionFactory("v11.0")); 
        } 
    }
}
