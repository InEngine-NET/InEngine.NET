using Common.Logging;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.Storage;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;
using MSMessageQueue = System.Messaging.MessageQueue;

namespace IntegrationEngine.MessageQueue
{
    public class MsmqListener : IMessageQueueListener
    {
        public IList<Type> IntegrationJobTypes { get; set; }
        public ILog Log { get; set; }
        public IMailClient MailClient { get; set; }
        public IntegrationEngineContext IntegrationEngineContext { get; set; }
        public IElasticClient ElasticClient { get; set; }
        public MSMessageQueue MSMessageQueue { get; set; }
        string _queueName { get; set; }
        public string QueueName
        {
            get { return _queueName; }
            set
            {
                if (!MSMessageQueue.Exists(QueueName))
                    MSMessageQueue.Create(QueueName);
                _queueName = value;
            }
        }

        public void Listen()
        {
            Message newMessage = MSMessageQueue.Receive();
            var body = newMessage.Body as byte[];
            var message = Encoding.UTF8.GetString(body);
            Log.Debug(x => x("Message queue listener received {0}", message));
            if (IntegrationJobTypes != null && !IntegrationJobTypes.Any())
                return;
            var type = IntegrationJobTypes.FirstOrDefault(t => t.FullName.Equals(message));
            var integrationJob = Activator.CreateInstance(type) as IIntegrationJob;
            integrationJob = AutoWireJob(integrationJob, type);
            try
            {
                if (integrationJob != null)
                    integrationJob.Run();
            }
            catch (Exception exception)
            {
                Log.Error(x => x("Integration job did not run successfully ({0})}", message), exception);
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
