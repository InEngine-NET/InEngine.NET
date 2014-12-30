using System;
using RabbitMQ.Client;

namespace IntegrationEngine
{
    public class MessageQueueConnection : IMessageQueueConnection
    {
        public MessageQueueConfiguration MessageQueueConfiguration { get; set; }
        public ConnectionFactory ConnectionFactory { get; set; }

        public MessageQueueConnection()
        {
        }

        public MessageQueueConnection(MessageQueueConfiguration messageQueueConfiguration) : this()
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
            if (ConnectionFactory == null)
                ConnectionFactory = GetConnectionFactory();
            return ConnectionFactory.CreateConnection();
        }
    }
}

