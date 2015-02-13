using IntegrationEngine.Core.Configuration;
using MySql.Data.MySqlClient;
using System.Data.Entity;
using System.Data.SqlClient;

namespace IntegrationEngine.Core.Storage
{
    public class DatabaseInitializer
    {
//        string _connectionString;
//        public string ConnectionString { get { return _connectionString; } }
//        public DatabaseConfiguration DatabaseConfiguration { get; private set; }
//
//        public DatabaseInitializer()
//        {
//            _connectionString = "";
//            Initialize();
//        }
//
//        public DatabaseInitializer(DatabaseConfiguration databaseConfiguration) : this()
//        {
//            DatabaseConfiguration = databaseConfiguration;
//        }
//
//        public IntegrationEngineContext GetDbContext()
//        {
//            Initialize();
//            return new IntegrationEngineContext(_connectionString);
//        }
//
//        public void Initialize()
//        {
//            if (DatabaseConfiguration == null)
//                return;
//            DbConfiguration.SetConfiguration(new IntegrationEngineDbConfiguration(DatabaseConfiguration.ServerType));
//            if (DatabaseConfiguration.ServerType == "MySQL")
//                _connectionString = GetMySqlConnectionString();
//            if (DatabaseConfiguration.ServerType == "SQLServer")
//                _connectionString = GetSqlServerConnectionString();
//        }
//
//        string GetMySqlConnectionString()
//        {
//            return (new MySqlConnectionStringBuilder()
//            {
//                Server = DatabaseConfiguration.HostName,
//                Port = DatabaseConfiguration.Port,
//                Database = DatabaseConfiguration.DatabaseName,
//                UserID = DatabaseConfiguration.UserName,
//                Password = DatabaseConfiguration.Password,
//            }).ConnectionString;
//        }
//
//        string GetSqlServerConnectionString()
//        {
//            return (new SqlConnectionStringBuilder()
//            {
//                DataSource = string.Join(",", DatabaseConfiguration.HostName, DatabaseConfiguration.Port),
//                InitialCatalog = DatabaseConfiguration.DatabaseName,
//                IntegratedSecurity = false,
//                UserID = DatabaseConfiguration.UserName,
//                Password = DatabaseConfiguration.Password,
//            }).ConnectionString;
//        }
    }
}
