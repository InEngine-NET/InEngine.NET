using System.Collections.Generic;

namespace InEngine.Core.Queuing
{
    public interface IQueueClient
    {
        string QueueBaseName { get; set; }
        string QueueName { get; set; }
        bool UseCompression { get; set; }
        void Publish(AbstractCommand command);
        ICommandEnvelope Consume();
        long GetPendingQueueLength();
        long GetInProgressQueueLength();
        long GetFailedQueueLength();
        bool ClearPendingQueue();
        bool ClearInProgressQueue();
        bool ClearFailedQueue();
        void RepublishFailedMessages();
        List<ICommandEnvelope> PeekPendingMessages(long from, long to);
        List<ICommandEnvelope> PeekInProgressMessages(long from, long to);
        List<ICommandEnvelope> PeekFailedMessages(long from, long to);
    }
}
