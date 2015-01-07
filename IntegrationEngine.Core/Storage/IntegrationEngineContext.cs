using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations.History;
using System.Data.Entity.ModelConfiguration.Conventions;
using MySql.Data.Entity;
using IntegrationEngine.Core.Models;
using System.Data.Entity.ModelConfiguration.Configuration;

namespace IntegrationEngine.Core.Storage
{
    public class IntegrationEngineContext : DbContext
    {
        public IntegrationEngineContext(string connectionString)
            : base(connectionString)
        { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Add<PluralizingTableNameConvention>();
        }
    }
}
