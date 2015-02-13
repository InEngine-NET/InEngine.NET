using Common.Logging;
using IntegrationEngine.Core.MessageQueue;
using System.Collections.Generic;

namespace IntegrationEngine.Scheduler
{
    public interface IDispatcher
    {
        IMessageQueueClient MessageQueueClient { get; set; }
        ILog Log { get; set; }
        void Dispatch<T>(T value, IDictionary<string, string> parameters);
    }
}
