using System;
using System.Data.Entity;
using IntegrationEngine.Jobs;

namespace IntegrationEngine.ConsoleHost
{
    public class IntegrationEngineContext : DbContext
    {
        public DbSet<MailMessageJob> EmailJobs { get; set; }
    }
}
