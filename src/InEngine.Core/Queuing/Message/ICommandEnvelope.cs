using System;

namespace InEngine.Core.Queuing.Message
{
    public interface ICommandEnvelope
    {
        int Id { get; set; }
        string PluginName { get; set; }
        string CommandClassName { get; set; }
        string SerializedCommand { get; set; }
        DateTime QueuedAt { get; set; }
        bool IsCompressed { get; set; }
    }
}
