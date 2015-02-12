using Common.Logging;
using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.Jobs;
using Newtonsoft.Json;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace IntegrationEngine.Core.MessageQueue
{
    public class RabbitMQClient : IMessageQueueClient
    {
        public RabbitMQConfiguration MessageQueueConfiguration { get; set; }
        public IMessageQueueConnection MessageQueueConnection { get; set; }
        public ILog Log { get; set; }

        public RabbitMQClient() 
        {
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public RabbitMQClient(RabbitMQConfiguration messageQueueConfiguration) : this()
        {
            MessageQueueConnection = new MessageQueueConnection(messageQueueConfiguration); 
            MessageQueueConfiguration = messageQueueConfiguration;
        }

        public void Publish(byte[] message)
        {
            using (var connection = MessageQueueConnection.GetConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueBind(MessageQueueConfiguration.QueueName, MessageQueueConfiguration.ExchangeName, "");
                channel.BasicPublish(MessageQueueConfiguration.ExchangeName, "", null, message);
            }
        }

        public bool IsServerAvailable()
        {
            try 
            {
                return MessageQueueConnection.GetConnection().IsOpen;
            }
            catch(BrokerUnreachableException exception) 
            {
                Log.Error(exception);
                return false;
            }
        }
    }
}
