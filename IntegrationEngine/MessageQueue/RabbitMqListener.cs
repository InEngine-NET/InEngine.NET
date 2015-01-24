using Common.Logging;
using IntegrationEngine.Configuration;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.Storage;
using Nest;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntegrationEngine.MessageQueue
{
    public class RabbitMQListener : IMessageQueueListener
    {
        public IList<Type> IntegrationJobTypes { get; set; }
        public MessageQueueConfiguration MessageQueueConfiguration { get; set; }
        public MessageQueueConnection MessageQueueConnection { get; set; }
        public ILog Log { get; set; }
        public IMailClient MailClient { get; set; }
        public IntegrationEngineContext IntegrationEngineContext { get; set; }
        public IElasticClient ElasticClient { get; set; }

        public RabbitMQListener()
        {}

        public void Listen()
        {
            var connection = MessageQueueConnection.GetConnection();
            using (var channel = connection.CreateModel())
            {
                var consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(MessageQueueConfiguration.QueueName, true, consumer);

                Log.Info(x => x("Waiting for messages..."));
                while (true)
                {
                    var eventArgs = (BasicDeliverEventArgs)consumer.Queue.Dequeue();
                    var body = eventArgs.Body;
                    var message = JsonConvert.DeserializeObject<DispatchMessage>(Encoding.UTF8.GetString(body));
                    Log.Debug(x => x("Message queue listener received {0}", message));
                    if (IntegrationJobTypes != null && !IntegrationJobTypes.Any())
                        continue;
                    var type = IntegrationJobTypes.FirstOrDefault(t => t.FullName.Equals(message.JobTypeName));
                    var integrationJob = Activator.CreateInstance(type) as IIntegrationJob;
                    integrationJob = AutoWireJob(integrationJob, type);
                    try
                    {
                        if (integrationJob != null)
                        {
                            if (integrationJob.GetType() is IParameterizedJob)
                                (integrationJob as IParameterizedJob).Parameters = message.Parameters;
                            integrationJob.Run();
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Error(x => x("Integration job did not run successfully ({0})}", message), exception);
                    }
                }
            }
        }

        T AutoWireJob<T>(T job, Type type)
        {
            if (type.GetInterface(typeof(IMailJob).Name) != null)
                (job as IMailJob).MailClient = MailClient;
            if (type.GetInterface(typeof(ISqlJob).Name) != null)
                (job as ISqlJob).DbContext = IntegrationEngineContext;
            if (type.GetInterface(typeof(ILogJob).Name) != null)
                (job as ILogJob).Log = Log;
            if (type.GetInterface(typeof(IElasticsearchJob).Name) != null)
                (job as IElasticsearchJob).ElasticClient = ElasticClient;
            return job;
        }
    }
}
