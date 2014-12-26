using System;

namespace TryQuartz.MessageQueue
{
    public interface IMessageQueueClient
    {
        void Publish(string message);
    }
}
