using IntegrationEngine.Configuration;
using RabbitMQ.Client;

namespace IntegrationEngine.MessageQueue
{
    public interface IMessageQueueConnection
    {
        RabbitMQConfiguration MessageQueueConfiguration { get; set; } 
        ConnectionFactory GetConnectionFactory();
        IConnection GetConnection();
    }
}

