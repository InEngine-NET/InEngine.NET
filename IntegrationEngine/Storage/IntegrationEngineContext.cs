using IntegrationEngine.Models;
using MySql.Data.Entity;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace IntegrationEngine.Storage
{
    //[DbConfigurationType("IntegrationEngine.Storage.IntegrationEngineDbConfiguration, IntegrationEngine")]
    public class IntegrationEngineContext : DbContext
    {
        //public IntegrationEngineContext()
        //{ }
        public IntegrationEngineContext(string connectionString)
            : base(connectionString)
        { }
        //public IntegrationEngineContext(DbConnection dbConnection, bool contextOwnsConnection = true)
        //    : base(dbConnection, contextOwnsConnection) 
        //{}

        public DbSet<MailMessageJob> MailMessageJobs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Add<PluralizingTableNameConvention>();
            base.OnModelCreating(modelBuilder);
        }
    }
}
