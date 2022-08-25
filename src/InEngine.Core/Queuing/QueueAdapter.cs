using System;
using System.Collections.Generic;
using System.Threading;
using Common.Logging;
using InEngine.Core.IO;
using InEngine.Core.Queuing.Clients;
using InEngine.Core.Queuing.Message;

namespace InEngine.Core.Queuing
{
    public class QueueAdapter : IQueueClient
    {
        public ILog Log { get; set; } = LogManager.GetLogger<QueueAdapter>();
        public int Id { get { return QueueClient.Id; } set { QueueClient.Id = value; } }
        public IQueueClient QueueClient { get; set; }
        public string QueueBaseName { get => QueueClient.QueueBaseName; set => QueueClient.QueueBaseName = value; }
        public string QueueName { get => QueueClient.QueueName; set => QueueClient.QueueName = value; }
        public bool UseCompression { get => QueueClient.UseCompression; set => QueueClient.UseCompression = value; }
        public MailSettings MailSettings { get => QueueClient.MailSettings; set => QueueClient.MailSettings = value; }

        public static QueueAdapter Make(bool useSecondaryQueue, QueueSettings queueSettings, MailSettings mailSettings)
        {
            var queueDriverName = queueSettings.QueueDriver.ToLower();
            var queue = new QueueAdapter();

            if (queueDriverName == "redis") {
                RedisClient.ClientSettings = queueSettings.Redis;
                queue.QueueClient = new RedisClient() {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression,
                };   
            }
            else if (queueDriverName == "rabbitmq") {
                RabbitMQClient.ClientSettings = queueSettings.RabbitMQ;
                queue.QueueClient = new RabbitMQClient() {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression
                };
            }
            else if (queueDriverName == "file") {
                FileClient.ClientSettings = queueSettings.File;
                queue.QueueClient = new FileClient() {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression
                };
            }
            else if (queueDriverName == "sync")
                queue.QueueClient = new SyncClient();
            else
                throw new Exception("Unspecified or unknown queue driver.");

            queue.QueueName = useSecondaryQueue ? "Secondary" : "Primary";
            queue.MailSettings = mailSettings;
            return queue;
        }

        public void Publish(AbstractCommand command)
        {
            QueueClient.Publish(command);
        }

        public void Consume(CancellationToken CancellationToken)
        {
            QueueClient.Consume(CancellationToken);
        }

        public ICommandEnvelope Consume()
        {
            return QueueClient.Consume();
        }

        public void Recover()
        {
            QueueClient.Recover();
        }

        public bool ClearPendingQueue()
        {
            return QueueClient.ClearPendingQueue();
        }

        public bool ClearInProgressQueue()
        {
            return QueueClient.ClearInProgressQueue();
        }

        public bool ClearFailedQueue()
        {
            return QueueClient.ClearFailedQueue();
        }

        public void RepublishFailedMessages()
        {
            QueueClient.RepublishFailedMessages();
        }

        public List<ICommandEnvelope> PeekPendingMessages(long from, long to)
        {
            return QueueClient.PeekPendingMessages(from, to);
        }

        public List<ICommandEnvelope> PeekInProgressMessages(long from, long to)
        {
            return QueueClient.PeekInProgressMessages(from, to);
        }

        public List<ICommandEnvelope> PeekFailedMessages(long from, long to)
        {
            return QueueClient.PeekFailedMessages(from, to);
        }

        public Dictionary<string, long> GetQueueLengths()
        {
            return QueueClient.GetQueueLengths();
        }
    }
}
