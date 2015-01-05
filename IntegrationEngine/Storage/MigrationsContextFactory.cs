using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Storage
{
    public class MigrationsContextFactory : IDbContextFactory<IntegrationEngineContext>
    {
        public IntegrationEngineContext Create()
        {
            var databaseConfiguration = (new EngineJsonConfiguration()).DatabaseConfiguration;
            var databaseInitializer = new DatabaseInitializer(databaseConfiguration);
            return databaseInitializer.GetDbContext();
        }
    }
}
