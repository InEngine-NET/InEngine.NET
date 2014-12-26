using System;

namespace TryQuartz
{
    public interface IMessageQueueClient
    {
        void Publish(string message);
    }
}
