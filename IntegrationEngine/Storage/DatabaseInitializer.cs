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
            var connectionString = "";
            if (DatabaseConfiguration.ServerType == "MySQL")
            {
                DbConfiguration.SetConfiguration(new MySqlEFConfiguration());
                connectionString = (new MySqlConnectionStringBuilder()
                {
                    Server = DatabaseConfiguration.HostName,
                    Port = DatabaseConfiguration.Port,
                    Database = DatabaseConfiguration.DatabaseName,
                    UserID = DatabaseConfiguration.UserName,
                    Password = DatabaseConfiguration.Password,
                }).ConnectionString;
            }
            if (DatabaseConfiguration.ServerType == "SQLServer")
            {
                DbConfiguration.SetConfiguration(new SqlServerDbConfiguration());
                connectionString = (new SqlConnectionStringBuilder() {
                    DataSource = string.Join(",", DatabaseConfiguration.HostName, DatabaseConfiguration.Port),
                    InitialCatalog = DatabaseConfiguration.DatabaseName,
                    UserID = DatabaseConfiguration.UserName,
                    Password = DatabaseConfiguration.Password,
                }).ConnectionString;
            }
            _connectionString = connectionString;
            return new IntegrationEngineContext(connectionString);
        }
    }
}
