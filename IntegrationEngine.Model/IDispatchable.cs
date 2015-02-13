using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Model
{
    public interface IDispatchable : IHasParameters
    {
        string JobType { get; set; }
    }
}
