using System;
using System.Collections.Generic;
using System.Reflection;
using InEngine.Core.Exceptions;
using InEngine.Core.Queuing.Clients;
using InEngine.Core.Queuing.Message;

namespace InEngine.Core.Queuing
{
    public class QueueAdapter : IQueueClient
    {
        public IQueueClient QueueClient { get; set; }
        public string QueueBaseName { get => QueueClient.QueueBaseName; set => QueueClient.QueueBaseName = value; }
        public string QueueName { get => QueueClient.QueueName; set => QueueClient.QueueName = value; }
        public bool UseCompression { get => QueueClient.UseCompression; set => QueueClient.UseCompression = value; }

        public static QueueAdapter Make(bool useSecondaryQueue = false)
        {
            var queueSettings = InEngineSettings.Make().Queue;
            var queueDriverName = queueSettings.QueueDriver.ToLower();
            var queue = new QueueAdapter();

            if (queueDriverName == "redis")
                queue.QueueClient = new RedisClient()
                {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression,
                    RedisDb = queueSettings.RedisDb
                };
            else if (queueDriverName == "file")
                queue.QueueClient = new FileClient() {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression,
                };
            else if (queueDriverName == "sync")
                queue.QueueClient = new SyncClient();
            else
                throw new Exception("Unspecified or unknown queue driver.");

            queue.QueueClient.QueueName = useSecondaryQueue ? "Secondary" : "Primary";
            return queue;
        }

        public void Publish(AbstractCommand command)
        {
            QueueClient.Publish(command);
        }

        public ICommandEnvelope Consume()
        {
            return QueueClient.Consume();
        }

        public static AbstractCommand ExtractCommandInstanceFromMessage(ICommandEnvelope commandEnvelope)
        {
            var commandType = Type.GetType($"{commandEnvelope.CommandClassName}, {commandEnvelope.CommandAssemblyName}");
            if (commandType == null)
                throw new CommandFailedException($"Could not locate command {commandEnvelope.CommandClassName}. Is the {commandEnvelope.CommandAssemblyName} plugin registered in the settings file?");
            return commandEnvelope.SerializedCommand.DeserializeFromJson<AbstractCommand>(commandEnvelope.IsCompressed);
        }

        public long GetPendingQueueLength()
        {
            return QueueClient.GetPendingQueueLength();
        }

        public long GetInProgressQueueLength()
        {
            return QueueClient.GetInProgressQueueLength();
        }

        public long GetFailedQueueLength()
        {
            return QueueClient.GetFailedQueueLength();
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
    }
}
