using IntegrationEngine.Models;
using MySql.Data.Entity;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace IntegrationEngine.Storage
{
    public class IntegrationEngineContext : DbContext
    {
        public IntegrationEngineContext(string connectionString)
            : base(connectionString)
        { }

        //public DbSet<MailMessageJob> MailMessageJobs { get; set; }
        public DbSet<IntegrationJob> IntegrationJobs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Add<PluralizingTableNameConvention>();
            //modelBuilder.Entity<IntegrationJob>().Property(p => p.StartTimeUtc).HasColumnType("DateTimeOffset");
            base.OnModelCreating(modelBuilder);
        }
    }
}
