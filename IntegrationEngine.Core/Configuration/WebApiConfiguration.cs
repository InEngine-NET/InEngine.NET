using System.Collections.Generic;

namespace IntegrationEngine.Core.Configuration
{
    public class WebApiConfiguration
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public List<string> Origins { get; set; }
    }
}
