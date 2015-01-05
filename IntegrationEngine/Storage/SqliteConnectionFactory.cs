using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Data.SQLite;

namespace IntegrationEngine.Storage
{
    class SqliteConnectionFactory : IDbConnectionFactory
    {
        public DbConnection CreateConnection(string nameOrConnectionString)
        {
            return new SQLiteConnection(nameOrConnectionString);
        }
    }
}
