using System;
using RabbitMQ.Client;

namespace IntegrationEngine
{
    public interface IMessageQueueConnection
    {
        MessageQueueConfiguration MessageQueueConfiguration { get; set; } 
        ConnectionFactory GetConnectionFactory();
    }
}

