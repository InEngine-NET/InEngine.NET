using System;
using System.Collections.Generic;

namespace IntegrationEngine.MessageQueue
{
    public interface IMessageQueueClient
    {
        void Publish<T>(T value, IDictionary<string, string> parameters);
        bool IsServerAvailable();
    }
}
