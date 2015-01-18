using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Configuration
{
    public class WebApiConfiguration
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public IList<string> Origins { get; set; }
    }
}
