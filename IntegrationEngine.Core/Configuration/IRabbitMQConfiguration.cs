using System;

namespace IntegrationEngine.Core.Configuration
{
    public interface IRabbitMQConfiguration
    {
        string QueueName { get; set; }
        string ExchangeName { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
        string HostName { get; set; }
        string VirtualHost { get; set; }
    }
}

