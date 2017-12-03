using System.Data.Common;
using System.Data.Entity;

namespace InEngine.Core.Queuing.Clients.Database
{
    public class QueueDbContext : DbContext
    {
        public DbSet<CommandEnvelopeModel> Messages { get; set; }
        public string MessageTableName { get; set; }

        public QueueDbContext(string messageTableName) 
        {
            MessageTableName = messageTableName;
        }

        public QueueDbContext(DbConnection existingConnection, bool contextOwnsConnection, string messageTableName) 
            : base(existingConnection, contextOwnsConnection)
        {
            MessageTableName = messageTableName;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CommandEnvelopeModel>().ToTable(MessageTableName);
        }
    }
}
