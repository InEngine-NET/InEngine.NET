using IntegrationEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Scheduler
{
    public interface IHasStateDescription : IHasStateId
    {
        string StateDescription { get; }
    }
}
