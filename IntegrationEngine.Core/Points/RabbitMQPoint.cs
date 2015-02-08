using System;

namespace IntegrationEngine.Core.Points
{
    public class RabbitMQPoint : IRabbitMQPoint
    {
        public string QueueName { get; set; }
        public string ExchangeName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string HostName { get; set; }
        public string VirtualHost { get; set; }

        public RabbitMQPoint()
        {
        }
    }
}

