using System;
using System.Collections.Generic;
using InEngine.Core.Exceptions;
using InEngine.Core.Queuing.Clients;
using Newtonsoft.Json;

namespace InEngine.Core.Queuing
{
    public class Queue : IQueueClient
    {
        public IQueueClient QueueClient { get; set; }
        public string QueueBaseName { get => QueueClient.QueueBaseName; set => QueueClient.QueueBaseName = value; }
        public string QueueName { get => QueueClient.QueueName; set => QueueClient.QueueName = value; }
        public string PendingQueueName { get => QueueClient.PendingQueueName; }
        public string InProgressQueueName { get => QueueClient.InProgressQueueName; }
        public string FailedQueueName { get => QueueClient.FailedQueueName; }
        public bool UseCompression { get => QueueClient.UseCompression; set => QueueClient.UseCompression = value; }

        public static Queue Make(bool useSecondaryQueue = false)
        {
            var queueSettings = InEngineSettings.Make().Queue;
            var queueDriverName = queueSettings.QueueDriver.ToLower();
            var queue = new Queue();

            if (queueDriverName == "redis")
            {
                queue.QueueClient = new RedisClient() {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression,
                    RedisDb = queueSettings.RedisDb
                };
            }
            else if (queueDriverName == "database")
            { 
                queue.QueueClient = new DatabaseClient() {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression,
                };
            }
            else
            { 
                queue.QueueClient = new InMemoryClient() {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression,
                };
            }

            queue.QueueClient.QueueName = useSecondaryQueue ? "Secondary" : "Primary";
            return queue;
        }

        public void Publish(ICommand command)
        {
            throw new NotImplementedException();
        }

        public bool Consume()
        {
            throw new NotImplementedException();
        }

        public static ICommand ExtractCommandInstanceFromMessage(IMessage message)
        {
            var commandType = Type.GetType($"{message.CommandClassName}, {message.CommandAssemblyName}");
            if (commandType == null)
                throw new CommandFailedException("Could not locate command type.");
            if (message.IsCompressed)
                return JsonConvert.DeserializeObject(message.SerializedCommand.Decompress(), commandType) as ICommand;
            return JsonConvert.DeserializeObject(message.SerializedCommand, commandType) as ICommand;
        }

        public long GetPendingQueueLength()
        {
            throw new NotImplementedException();
        }

        public long GetInProgressQueueLength()
        {
            throw new NotImplementedException();
        }

        public long GetFailedQueueLength()
        {
            throw new NotImplementedException();
        }

        public bool ClearPendingQueue()
        {
            throw new NotImplementedException();
        }

        public bool ClearInProgressQueue()
        {
            throw new NotImplementedException();
        }

        public bool ClearFailedQueue()
        {
            throw new NotImplementedException();
        }

        public void RepublishFailedMessages()
        {
            throw new NotImplementedException();
        }

        public List<IMessage> PeekPendingMessages(long from, long to)
        {
            throw new NotImplementedException();
        }

        public List<IMessage> PeekInProgressMessages(long from, long to)
        {
            throw new NotImplementedException();
        }

        public List<IMessage> PeekFailedMessages(long from, long to)
        {
            throw new NotImplementedException();
        }

        public List<IMessage> GetMessages(string queueName, long from, long to)
        {
            throw new NotImplementedException();
        }
    }
}
