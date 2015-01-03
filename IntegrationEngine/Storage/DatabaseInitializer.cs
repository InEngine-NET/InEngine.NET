using MySql.Data.Entity;
using MySql.Data.MySqlClient;
using System.Data.Entity;
using System.Data.SqlClient;

namespace IntegrationEngine.Storage
{
    class DatabaseInitializer
    {
        string _connectionString;
        public string ConnectionString { get { return _connectionString; } }
        public DatabaseConfiguration DatabaseConfiguration { get; private set; }

        public DatabaseInitializer()
        {
            _connectionString = "";
        }

        public DatabaseInitializer(DatabaseConfiguration databaseConfiguration) : this()
        {
            DatabaseConfiguration = databaseConfiguration;
        }

        public IntegrationEngineContext GetDbContext()
        {
            DbConfiguration.SetConfiguration(new IntegrationEngineDbConfiguration(DatabaseConfiguration.ServerType));
            if (DatabaseConfiguration.ServerType == "MySQL")
                _connectionString = GetMySqlConnectionString();
            if (DatabaseConfiguration.ServerType == "SQLServer")
                _connectionString = GetSqlServerConnectionString();
            var dbContext = new IntegrationEngineContext(_connectionString);           
            dbContext.Database.CreateIfNotExists();
            return dbContext;
        }

        string GetMySqlConnectionString()
        {
            return (new MySqlConnectionStringBuilder()
            {
                Server = DatabaseConfiguration.HostName,
                Port = DatabaseConfiguration.Port,
                Database = DatabaseConfiguration.DatabaseName,
                UserID = DatabaseConfiguration.UserName,
                Password = DatabaseConfiguration.Password,
            }).ConnectionString;
        }

        string GetSqlServerConnectionString()
        {
            return (new SqlConnectionStringBuilder()
            {
                DataSource = string.Join(",", DatabaseConfiguration.HostName, DatabaseConfiguration.Port),
                InitialCatalog = DatabaseConfiguration.DatabaseName,
                IntegratedSecurity = false,
                UserID = DatabaseConfiguration.UserName,
                Password = DatabaseConfiguration.Password,
            }).ConnectionString;
        }
    }
}
