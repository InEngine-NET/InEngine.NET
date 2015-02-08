using System;
using System.Threading;

namespace IntegrationEngine.MessageQueue
{
    public interface IMessageQueueListener : IDisposable
    {
        void Listen(CancellationToken cancellationToken);
    }
}
