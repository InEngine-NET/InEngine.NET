using System;
using FX.Configuration;

namespace IntegrationEngine
{
    public class EngineJsonConfiguration : JsonConfiguration
    {
        public EngineJsonConfiguration() : base("IntegrationEngine.json")
        {
        }

        public MessageQueueConfiguration MessageQueueConfiguration { get; set; }
        public MailConfiguration MailConfiguration { get; set; }
        public DatabaseConfiguration DatabaseConfiguration { get; set; }
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

    public class MailConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }

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

