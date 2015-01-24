using IntegrationEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.MessageQueue
{
    public class DispatchMessage : IHasParameters
    {
        public string JobTypeName { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
    }
}
