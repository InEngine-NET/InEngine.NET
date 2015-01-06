using IntegrationEngine.Configuration;
using IntegrationEngine.Jobs;
using IntegrationEngine.Mail;
using IntegrationEngine.Storage;
using log4net;
using Nest;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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

                Log.Info("Waiting for messages...");
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
                    var integrationJob = Activator.CreateInstance(type) as IIntegrationJob;
                    integrationJob = AutoWireJob(integrationJob, type);
                    if (integrationJob != null)
                        integrationJob.Run();
                }
            }
        }

        T AutoWireJob<T>(T job, Type type)
        {
            if (type.GetInterface(typeof(IMailJob).Name) != null)
                (job as IMailJob).MailClient = Container.Resolve<IMailClient>();
            if (type.GetInterface(typeof(ISqlJob).Name) != null)
                (job as ISqlJob).DbContext = Container.Resolve<IntegrationEngineContext>();
            if (type.GetInterface(typeof(ILogJob).Name) != null)
                (job as ILogJob).Log = Container.Resolve<ILog>();
            if (type.GetInterface(typeof(IElasticsearchJob).Name) != null)
                (job as IElasticsearchJob).ElasticClient = Container.Resolve<IElasticClient>();
            return job;
        }
    }
}
