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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace IntegrationEngine.MessageQueue
{
    public class RabbitMQListener : IMessageQueueListener
    {
        Thread listenerThread;
        volatile bool shouldTerminate;
        QueueingBasicConsumer consumer;
        public IList<Type> IntegrationJobTypes { get; set; }
        public MessageQueueConfiguration MessageQueueConfiguration { get; set; }
        public MessageQueueConnection MessageQueueConnection { get; set; }
        public ILog Log { get; set; }
        public IMailClient MailClient { get; set; }
        public IntegrationEngineContext IntegrationEngineContext { get; set; }
        public IElasticClient ElasticClient { get; set; }

        public RabbitMQListener()
        {
            shouldTerminate = false;
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void Dispose()
        {
            shouldTerminate = true;
            if (consumer != null)
                consumer.Queue.Close();
            listenerThread.Join();
        }

        void _listen()
        {
            var connection = MessageQueueConnection.GetConnection();
            using (var channel = connection.CreateModel())
            {
                consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(MessageQueueConfiguration.QueueName, true, consumer);
                Log.Info(x => x("Waiting for messages..."));

                while (true)
                {
                    var message = new DispatchMessage();
                    try
                    {
                        if (shouldTerminate)
                        {
                            connection.Close();
                            Log.Info("Message queue listener has stopped listening for messages.");
                            return;
                        }
                        var eventArgs = (BasicDeliverEventArgs)consumer.Queue.Dequeue();
                        var body = eventArgs.Body;
                        message = JsonConvert.DeserializeObject<DispatchMessage>(Encoding.UTF8.GetString(body));
                        Log.Debug(x => x("Message queue listener received {0}", message));
                        if (IntegrationJobTypes != null && !IntegrationJobTypes.Any())
                            continue;
                        var type = IntegrationJobTypes.FirstOrDefault(t => t.FullName.Equals(message.JobTypeName));
                        var integrationJob = Activator.CreateInstance(type) as IIntegrationJob;
                        integrationJob = AutoWireJob(integrationJob, type);
                        if (integrationJob != null)
                        {
                            if (integrationJob is IParameterizedJob)
                                (integrationJob as IParameterizedJob).Parameters = message.Parameters;
                            integrationJob.Run();
                        }
                    }
                    catch (IntegrationJobRunFailureException exception)
                    {
                        Log.Error(x => x("Integration job did not run successfully ({0}).", message.JobTypeName), exception);
                    }
                    catch (EndOfStreamException exception)
                    {
                        Log.Debug(x => x("The message queue ({0}) has closed.", MessageQueueConfiguration.QueueName), exception);
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Issue receiving/decoding dispatch message or running job.", exception);
                    }
                }
            }
        }

        public void Listen() 
        {
            if (listenerThread == null)
                listenerThread = new Thread(_listen);
            if (listenerThread.ThreadState == ThreadState.Running)
            {
                Log.Info("Message queue listener already running.");
                return;
            }

            listenerThread.Start();
            Log.Info("Message queue listener started.");
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
