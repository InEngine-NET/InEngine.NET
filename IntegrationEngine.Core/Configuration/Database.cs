using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Configuration
{
    public class DatabaseConfiguration
    {
        public string ServerType { get; set; }
        public string HostName { get; set; }
        public uint Port { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
