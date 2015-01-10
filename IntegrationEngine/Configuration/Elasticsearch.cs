using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FX.Configuration;

namespace IntegrationEngine.Configuration
{
    public class ElasticsearchConfiguration : JsonConfiguration
    {
        public string Protocol { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public string DefaultIndex { get; set; }
    }
}
