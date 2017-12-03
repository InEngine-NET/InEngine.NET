using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using InEngine.Core.Queuing.Clients;
using Newtonsoft.Json;
using Serialize.Linq.Extensions;

namespace InEngine.Core.Queuing
{
    public class Queue : IQueueClient
    {
        public IQueueClient QueueClient { get; set; }
        public string QueueBaseName { get => QueueClient.QueueBaseName; set => QueueClient.QueueBaseName = value; }
        public string QueueName { get => QueueClient.QueueName; set => QueueClient.QueueName = value; }
        public bool UseCompression { get => QueueClient.UseCompression; set => QueueClient.UseCompression = value; }

        public static Queue Make(bool useSecondaryQueue = false)
        {
            var queueSettings = InEngineSettings.Make().Queue;
            var queueDriverName = queueSettings.QueueDriver.ToLower();
            var queue = new Queue();

            if (queueDriverName == "redis")
                queue.QueueClient = new RedisClient()
                {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression,
                    RedisDb = queueSettings.RedisDb
                };
            else if (queueDriverName == "database")
                queue.QueueClient = new DatabaseClient() {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression,
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

        public void Publish(Expression<Action> expressionAction)
        {
            QueueClient.Publish(new Lambda(expressionAction.ToExpressionNode()));
        }

        public void Publish(ICommand command)
        {
            QueueClient.Publish(command);
        }

        public void Publish(IList<AbstractCommand> commands)
        {
            QueueClient.Publish(new Chain() { Commands = commands });
        }

        public bool Consume()
        {
            return QueueClient.Consume();
        }

        public static ICommand ExtractCommandInstanceFromMessage(IMessage message)
        {
            var commandType = Type.GetType($"{message.CommandClassName}, {message.CommandAssemblyName}");
            if (commandType == null)
                throw new CommandFailedException($"Could not locate command {message.CommandClassName}. Is the {message.CommandAssemblyName} plugin registered in the settings file?");
            return JsonConvert.DeserializeObject(
                message.IsCompressed ? message.SerializedCommand.Decompress() : message.SerializedCommand,
                commandType,
                new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects }
            ) as ICommand;
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

        public List<IMessage> PeekPendingMessages(long from, long to)
        {
            return QueueClient.PeekPendingMessages(from, to);
        }

        public List<IMessage> PeekInProgressMessages(long from, long to)
        {
            return QueueClient.PeekInProgressMessages(from, to);
        }

        public List<IMessage> PeekFailedMessages(long from, long to)
        {
            return QueueClient.PeekFailedMessages(from, to);
        }
    }
}
