using Common.Logging;
using IntegrationEngine.Configuration;
using IntegrationEngine.Core.Jobs;
using Newtonsoft.Json;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace IntegrationEngine.MessageQueue
{
    public class RabbitMQClient : IMessageQueueClient
    {
        public MessageQueueConfiguration MessageQueueConfiguration { get; set; }
        public IMessageQueueConnection MessageQueueConnection { get; set; }
        public ILog Log { get; set; }

        public RabbitMQClient() 
        {
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void Publish<T>(T value, IDictionary<string, string> parameters)
        {
            try
            {
                var type = value.GetType();
                if (type is IParameterizedJob)
                    (value as IParameterizedJob).Parameters = parameters;
                var connection = MessageQueueConnection.GetConnection();
                using (var channel = connection.CreateModel())
                {
                    var message = JsonConvert.SerializeObject(new DispatchMessage {
                        JobTypeName = type.FullName,
                        Parameters = parameters,
                    });
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
