using IntegrationEngine.Configuration;
using log4net;
using System;
using System.Text;

namespace IntegrationEngine.MessageQueue
{
    public class RabbitMqClient : IMessageQueueClient
    {
        public MessageQueueConfiguration MessageQueueConfiguration { get; set; }
        public MessageQueueConnection MessageQueueConnection { get; set; }
        public ILog Log { get; set; }

        public RabbitMqClient() 
        {
            Log = Container.Resolve<ILog>();
        }

        public void Publish<T>(T value)
        {
            var type = value.GetType();
            var message = String.Join(".", type.Namespace, type.Name);
            var connection = MessageQueueConnection.GetConnection();
            using (var channel = connection.CreateModel())
            {
                channel.QueueBind(MessageQueueConfiguration.QueueName, MessageQueueConfiguration.ExchangeName, "");
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(MessageQueueConfiguration.ExchangeName, "", null, body);
                Log.Info(string.Format("Sent {0}", message));
            }
        }
    }
}

