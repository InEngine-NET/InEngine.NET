using System;

namespace InEngine.Core.Queuing
{
    public interface IMessage
    {
        int Id { get; set; }
        string CommandAssemblyName { get; set; }
        string CommandClassName { get; set; }
        string SerializedCommand { get; set; }
        DateTime QueuedAt { get; set; }
        bool IsCompressed { get; set; }
    }
}
