using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Configuration
{
    public class MessageQueueConfiguration
    {
        public string QueueName { get; set; }
        public string ExchangeName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string HostName { get; set; }
        public string VirtualHost { get; set; }
        public string DefaultIndex { get; set; }
    }
}
