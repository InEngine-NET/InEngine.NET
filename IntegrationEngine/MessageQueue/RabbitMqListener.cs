using System;
using System.Linq;
using System.Reflection;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using IntegrationEngine.Reports;
using System.Collections.Generic;
using log4net;

namespace IntegrationEngine.MessageQueue
{
    public class RabbitMqListener
    {
        public IList<Assembly> AssembliesWithJobs { get; set; }
        public MessageQueueConfiguration MessageQueueConfiguration { get; set; }
        public MessageQueueConnection MessageQueueConnection { get; set; }
        public ILog Log { get; set; }

        public RabbitMqListener()
        {
            Log = Container.Resolve<ILog>();
        }

        public void Listen()
        {
            var connection = MessageQueueConnection.GetConnection();
            using (var channel = connection.CreateModel())
            {
                var consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(MessageQueueConfiguration.QueueName, true, consumer);

                Log.Info("Waiting for messages. To exit press CTRL+C");
                while (true)
                {
                    var eventArgs = (BasicDeliverEventArgs)consumer.Queue.Dequeue();
                    var body = eventArgs.Body;
                    var message = Encoding.UTF8.GetString(body);
                    Log.Info(string.Format("Received {0}", message));
                    var types = AssembliesWithJobs
                        .SelectMany(x => x.GetTypes())
                        .Where(x => typeof(IIntegrationJob).IsAssignableFrom(x) && x.IsClass);
                    if (!types.Any())
                        continue;
                    var type = types.FirstOrDefault(t => t.FullName.Equals(message));
                    var reportJob = Activator.CreateInstance(type) as IIntegrationJob;
                    if (reportJob != null)
                        reportJob.Run();
                }
            }
        }
    }
}
