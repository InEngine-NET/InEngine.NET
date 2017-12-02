using System;
using System.Collections.Generic;

namespace InEngine.Core.Queuing
{
    public interface IQueueClient
    {
        string QueueBaseName { get; set; }
        string QueueName { get; set; }
        bool UseCompression { get; set; }
        void Publish(ICommand command);
        void Publish(Action action);
        bool Consume();
        long GetPendingQueueLength();
        long GetInProgressQueueLength();
        long GetFailedQueueLength();
        bool ClearPendingQueue();
        bool ClearInProgressQueue();
        bool ClearFailedQueue();
        void RepublishFailedMessages();
        List<IMessage> PeekPendingMessages(long from, long to);
        List<IMessage> PeekInProgressMessages(long from, long to);
        List<IMessage> PeekFailedMessages(long from, long to);
    }
}
