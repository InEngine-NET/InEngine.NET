using System;
using FX.Configuration;

namespace IntegrationEngine
{
    public class EngineJsonConfiguration : JsonConfiguration
    {
        public MessageQueueConfiguration MessageQueueConfiguration { get; set; }
    }

    public class MessageQueueConfiguration
    {
        public string QueueName { get; set; }
        public string ExchangeName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string HostName { get; set; }
        public string VirtualHost { get; set; }
    }
}

