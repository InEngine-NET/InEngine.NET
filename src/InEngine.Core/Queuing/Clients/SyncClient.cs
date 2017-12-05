using System;
using System.Collections.Generic;

namespace InEngine.Core.Queuing.Clients
{
    public class SyncClient : IQueueClient
    {
        public string QueueBaseName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string QueueName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool UseCompression { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Publish(ICommand command)
        {
            command.Run();
        }

        public ICommandEnvelope Consume()
        {
            throw new NotImplementedException();
        }

        public bool ClearFailedQueue()
        {
            throw new NotImplementedException();
        }

        public bool ClearInProgressQueue()
        {
            throw new NotImplementedException();
        }

        public bool ClearPendingQueue()
        {
            throw new NotImplementedException();
        }

        public long GetFailedQueueLength()
        {
            throw new NotImplementedException();
        }

        public long GetInProgressQueueLength()
        {
            throw new NotImplementedException();
        }

        public long GetPendingQueueLength()
        {
            throw new NotImplementedException();
        }

        public List<ICommandEnvelope> PeekFailedMessages(long from, long to)
        {
            throw new NotImplementedException();
        }

        public List<ICommandEnvelope> PeekInProgressMessages(long from, long to)
        {
            throw new NotImplementedException();
        }

        public List<ICommandEnvelope> PeekPendingMessages(long from, long to)
        {
            throw new NotImplementedException();
        }

        public void RepublishFailedMessages()
        {
            throw new NotImplementedException();
        }
    }
}
