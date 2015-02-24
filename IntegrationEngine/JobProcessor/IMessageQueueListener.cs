using System;
using System.Threading;

namespace IntegrationEngine.JobProcessor
{
    public interface IMessageQueueListener : IDisposable
    {
        void Listen(CancellationToken cancellationToken, int listenerId);
    }
}
