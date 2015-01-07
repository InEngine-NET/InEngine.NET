using IntegrationEngine.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Jobs
{
    public interface ISqlJob : IIntegrationJob
    {

        IntegrationEngineContext DbContext { get; set; }
        string Query { get; set; }
        IList<T> RunQuery<T>();
    }
}
