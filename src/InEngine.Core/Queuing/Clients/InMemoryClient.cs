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
        public bool UseCompression { get; set; }

        public IDictionary<string,Queue<IMessage>> PendingQueue { get; set; }
        public IDictionary<string,List<IMessage>> InProgressMessages { get; set; }
        public IDictionary<string,List<IMessage>> FailedMessages { get; set; }

        public void Publish(ICommand command)
        {
            var serializedCommand = JsonConvert.SerializeObject(command);
            PendingQueue[QueueName].Enqueue(new Message {
                IsCompressed = UseCompression,
                CommandClassName = command.GetType().FullName,
                CommandAssemblyName = command.GetType().Assembly.GetName().Name + ".dll",
                SerializedCommand = UseCompression ? serializedCommand.Compress() : serializedCommand
            });
        }

        public bool Consume()
        {
            var message = PendingQueue[QueueName].Dequeue();
            if (message == null)
                return false;
            InProgressMessages[QueueName].Add(message);
            var commandInstance = Queue.ExtractCommandInstanceFromMessage(message);
            try
            {
                commandInstance.Run();
            }
            catch (Exception exception)
            {
                InProgressMessages[QueueName].Remove(message);
                FailedMessages[QueueName].Add(message);
                throw new CommandFailedException("Consumed command failed.", exception);
            }

            try
            {
                InProgressMessages[QueueName].Remove(message);
            }
            catch (Exception exception)
            {
                throw new CommandFailedException($"Failed to remove completed message from queue.", exception);
            }

            return true;
        }

        public long GetPendingQueueLength()
        {
            return PendingQueue[QueueName].Count;
        }

        public long GetInProgressQueueLength()
        {
            return InProgressMessages[QueueName].Count;
        }

        public long GetFailedQueueLength()
        {
            return FailedMessages[QueueName].Count;
        }

        public bool ClearPendingQueue()
        {
            PendingQueue[QueueName].Clear();
            return true;
        }

        public bool ClearInProgressQueue()
        {
            InProgressMessages[QueueName].Clear();
            return true;
        }

        public bool ClearFailedQueue()
        {
            FailedMessages[QueueName].Clear();
            return true;
        }

        public void RepublishFailedMessages()
        {
            FailedMessages[QueueName].ForEach(x => PendingQueue[QueueName].Enqueue(x));
            FailedMessages[QueueName].Clear();
        }

        public List<IMessage> PeekPendingMessages(long from, long to)
        {
            throw new NotImplementedException();
        }

        public List<IMessage> PeekInProgressMessages(long from, long to)
        {
            return InProgressMessages[QueueName].GetRange(Convert.ToInt32(from), Convert.ToInt32(to));
        }

        public List<IMessage> PeekFailedMessages(long from, long to)
        {
            return FailedMessages[QueueName].GetRange(Convert.ToInt32(from), Convert.ToInt32(to));
        }
    }
}
