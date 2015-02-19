using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace IntegrationEngine.Core.Storage
{
    public class IntegrationEngineContext : DbContext
    {
        public IntegrationEngineContext() { }

        public IntegrationEngineContext(string connectionString)
            : base(connectionString)
        { }
    }
}
