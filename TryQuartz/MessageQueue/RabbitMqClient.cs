using System;
using RabbitMQ.Client;
using System.Text;

namespace TryQuartz.MessageQueue
{
    public class RabbitMqClient : IMessageQueueClient
    {
        public ConnectionFactory ConnectionFactory { get; set; }

        public RabbitMqClient()
        {
            ConnectionFactory = new ConnectionFactory() { 
                HostName = "localhost",
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                Protocol = Protocols.DefaultProtocol,
                Port = AmqpTcpEndpoint.UseDefaultPort,
            };
        }

        public void Publish<T>(T value)
        {
            var type = value.GetType();
            var message = String.Join(".", type.Namespace, type.Name);
            using (var connection = ConnectionFactory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueBind("myqueue", "amq.fanout", "");
                    var body = Encoding.UTF8.GetBytes(message);
                    channel.BasicPublish("amq.fanout", "", null, body);
                    Console.WriteLine(" [x] Sent {0}", message);
                }
            }
        }
    }
}

