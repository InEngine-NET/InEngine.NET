using System;
using RabbitMQ.Client;

namespace IntegrationEngine.MessageQueue
{
    public interface IMessageQueueConnection
    {
        MessageQueueConfiguration MessageQueueConfiguration { get; set; } 
        ConnectionFactory GetConnectionFactory();
    }
}

