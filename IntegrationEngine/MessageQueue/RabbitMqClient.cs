using Common.Logging;
using IntegrationEngine.Configuration;
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
        {}

        public void Publish<T>(T value)
        {
            try
            {
                var type = value.GetType();
                var message = type.FullName;
                var connection = MessageQueueConnection.GetConnection();
                using (var channel = connection.CreateModel())
                {
                    channel.QueueBind(MessageQueueConfiguration.QueueName, MessageQueueConfiguration.ExchangeName, "");
                    var body = Encoding.UTF8.GetBytes(message);
                    channel.BasicPublish(MessageQueueConfiguration.ExchangeName, "", null, body);
                    Log.Debug(x => x("Sent message: {0}", message));
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }
    }
}

