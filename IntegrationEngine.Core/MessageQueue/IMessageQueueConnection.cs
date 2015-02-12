using IntegrationEngine.Core.Configuration;
using RabbitMQ.Client;

namespace IntegrationEngine.Core.MessageQueue
{
    public interface IMessageQueueConnection
    {
        IRabbitMQConfiguration MessageQueueConfiguration { get; set; } 
        ConnectionFactory GetConnectionFactory();
        IConnection GetConnection();
    }
}

