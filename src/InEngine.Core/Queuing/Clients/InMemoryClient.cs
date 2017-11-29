using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InEngine.Core.Exceptions;
using Newtonsoft.Json;

namespace InEngine.Core.Queuing.Clients
{
    public class InMemoryClient : IQueueClient
    {
        public string QueueBaseName { get; set; } = "InEngineQueue";
        public string QueueName { get; set; } = "Primary";
        public string PendingQueueName { get { return QueueBaseName + $":{QueueName}:Pending"; } }
        public string InProgressQueueName { get { return QueueBaseName + $":{QueueName}:InProgress"; } }
        public string FailedQueueName { get { return QueueBaseName + $":{QueueName}:Failed"; } }
        public bool UseCompression { get; set; }

        // todo make these into dictionaries for primary and secondary queues
        public Queue<IMessage> PendingQueue { get; set; }
        public List<IMessage> InProgressMessages { get; set; }
        public List<IMessage> FailedMessages { get; set; }

        public void Publish(ICommand command)
        {
            var serializedCommand = JsonConvert.SerializeObject(command);
            PendingQueue.Enqueue(new Message {
                IsCompressed = UseCompression,
                CommandClassName = command.GetType().FullName,
                CommandAssemblyName = command.GetType().Assembly.GetName().Name + ".dll",
                SerializedCommand = UseCompression ? serializedCommand.Compress() : serializedCommand
            });
        }

        public bool Consume()
        {
            var message = PendingQueue.Dequeue();
            if (message == null)
                return false;
            InProgressMessages.Add(message);
            var commandInstance = Queue.ExtractCommandInstanceFromMessage(message);
            try
            {
                commandInstance.Run();
            }
            catch (Exception exception)
            {
                InProgressMessages.Remove(message);
                FailedMessages.Add(message);
                throw new CommandFailedException("Consumed command failed.", exception);
            }

            try
            {
                InProgressMessages.Remove(message);
            }
            catch (Exception exception)
            {
                throw new CommandFailedException($"Failed to remove completed message from queue: {InProgressQueueName}", exception);
            }

            return true;
        }

        public long GetPendingQueueLength()
        {
            return PendingQueue.Count;
        }

        public long GetInProgressQueueLength()
        {
            return InProgressMessages.Count;
        }

        public long GetFailedQueueLength()
        {
            return FailedMessages.Count;
        }

        public bool ClearPendingQueue()
        {
            PendingQueue.Clear();
            return true;
        }

        public bool ClearInProgressQueue()
        {
            InProgressMessages.Clear();
            return true;
        }

        public bool ClearFailedQueue()
        {
            FailedMessages.Clear();
            return true;
        }

        public void RepublishFailedMessages()
        {
            FailedMessages.ForEach(x => PendingQueue.Enqueue(x));
            FailedMessages.Clear();
        }

        public List<IMessage> PeekPendingMessages(long from, long to)
        {
            throw new NotImplementedException();
        }

        public List<IMessage> PeekInProgressMessages(long from, long to)
        {
            return InProgressMessages.GetRange(Convert.ToInt32(from), Convert.ToInt32(to));
        }

        public List<IMessage> PeekFailedMessages(long from, long to)
        {
            return FailedMessages.GetRange(Convert.ToInt32(from), Convert.ToInt32(to));
        }
    }
}
