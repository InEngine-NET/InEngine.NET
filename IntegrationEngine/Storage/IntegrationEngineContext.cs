using IntegrationEngine.Models;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace IntegrationEngine.Storage
{
    public class IntegrationEngineContext : DbContext
    {
        public IntegrationEngineContext(string connectionString)
            : base(connectionString)
        {}

        public IntegrationEngineContext(DbConnection existingConnection, bool contextOwnsConnection)
          : base(existingConnection, contextOwnsConnection)
        {}

        public DbSet<MailMessage> MailMessages { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Add<PluralizingTableNameConvention>();
            base.OnModelCreating(modelBuilder);
        }
    }
}
