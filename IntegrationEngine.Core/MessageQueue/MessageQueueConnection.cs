using System;
using RabbitMQ.Client;
using IntegrationEngine.Core.Configuration;

namespace IntegrationEngine.Core.MessageQueue
{
    public class MessageQueueConnection : IMessageQueueConnection
    {
        public IRabbitMQConfiguration MessageQueueConfiguration { get; set; }
        public ConnectionFactory ConnectionFactory { get; set; }
        IConnection _connection;

        public MessageQueueConnection()
        {
        }

        public MessageQueueConnection(IRabbitMQConfiguration messageQueueConfiguration)
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
            if (_connection == null || !_connection.IsOpen) {
                if (ConnectionFactory == null)
                    ConnectionFactory = GetConnectionFactory();
                return _connection = ConnectionFactory.CreateConnection();
            }
            else 
                return _connection;
        }
    }
}

