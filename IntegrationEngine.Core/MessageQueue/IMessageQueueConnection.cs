using IntegrationEngine.Core.Configuration;
using RabbitMQ.Client;

namespace IntegrationEngine.Core.MessageQueue
{
    public interface IMessageQueueConnection
    {
        RabbitMQConfiguration MessageQueueConfiguration { get; set; } 
        ConnectionFactory GetConnectionFactory();
        IConnection GetConnection();
    }
}

