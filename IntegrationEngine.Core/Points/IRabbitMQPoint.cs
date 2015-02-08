using System;

namespace IntegrationEngine.Core.Points
{
    public interface IRabbitMQPoint
    {
        string QueueName { get; set; }
        string ExchangeName { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
        string HostName { get; set; }
        string VirtualHost { get; set; }
    }
}

