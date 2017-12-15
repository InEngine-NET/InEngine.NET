using System.Collections.Generic;
using System.Threading;
using Common.Logging;
using InEngine.Core.Queuing.Message;

namespace InEngine.Core.Queuing
{
    public interface IQueueClient
    {
        ILog Log { get; set; }
        int Id { get; set; }
        string QueueBaseName { get; set; }
        string QueueName { get; set; }
        bool UseCompression { get; set; }
        void Publish(AbstractCommand command);
        void Consume(CancellationToken cancellationToken);
        ICommandEnvelope Consume();
        void Recover();
        Dictionary<string, long> GetQueueLengths();
        bool ClearPendingQueue();
        bool ClearInProgressQueue();
        bool ClearFailedQueue();
        void RepublishFailedMessages();
        List<ICommandEnvelope> PeekPendingMessages(long from, long to);
        List<ICommandEnvelope> PeekInProgressMessages(long from, long to);
        List<ICommandEnvelope> PeekFailedMessages(long from, long to);
    }
}
