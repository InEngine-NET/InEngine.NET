using System;

namespace IntegrationEngine.MessageQueue
{
    public interface IMessageQueueClient
    {
        void Publish<T>(T value);
    }
}
