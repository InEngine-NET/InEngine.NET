using System;
using System.Collections.Generic;

namespace IntegrationEngine.Core.MessageQueue
{
    public interface IMessageQueueClient
    {
        void Publish(byte[] message);
        bool IsServerAvailable();
    }
}
