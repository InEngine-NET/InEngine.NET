using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Model
{
    public class DispatchTrigger : IDispatchable
    {
        public string JobType { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
    }
}
