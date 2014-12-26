using System;

namespace TryQuartz
{
    public interface IMessageQueueClientService
    {
        IMessageQueueClient MessageQueueClient { get; set; }
    }
}

