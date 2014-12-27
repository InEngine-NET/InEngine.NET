using System;

namespace TryQuartz.MessageQueue
{
    public interface IMessageQueueClient
    {
        void Publish<T>(T value);
    }
}
