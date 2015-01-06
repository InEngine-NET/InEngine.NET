using System;
using RabbitMQ.Client;
using IntegrationEngine.Configuration;

namespace IntegrationEngine.MessageQueue
{
    public class MessageQueueConnection : IMessageQueueConnection
    {
        public MessageQueueConfiguration MessageQueueConfiguration { get; set; }
        public ConnectionFactory ConnectionFactory { get; set; }
        IConnection _connection;

        public MessageQueueConnection()
        {
        }

        public MessageQueueConnection(MessageQueueConfiguration messageQueueConfiguration)
            : this()
        {
            MessageQueueConfiguration = messageQueueConfiguration;
        }

        public ConnectionFactory GetConnectionFactory()
        {
            return new ConnectionFactory() { 
                HostName = MessageQueueConfiguration.HostName,
                UserName = MessageQueueConfiguration.UserName,
                Password = MessageQueueConfiguration.Password,
                VirtualHost = MessageQueueConfiguration.VirtualHost,
                Protocol = Protocols.DefaultProtocol,
                Port = AmqpTcpEndpoint.UseDefaultPort,
            };
        }

        public IConnection GetConnection()
        {
            if (_connection != null)
                return _connection;
            if (ConnectionFactory == null)
                ConnectionFactory = GetConnectionFactory();
            return _connection = ConnectionFactory.CreateConnection();
        }
    }
}

