using System;

namespace InEngine.Core.Queuing.Message
{
    public class CommandEnvelope : ICommandEnvelope
    {
        public int Id { get; set; }
        public string PluginName { get; set; }
        public string CommandClassName { get; set; }
        public string SerializedCommand { get; set; }
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
        public bool IsCompressed { get; set; }
    }
}
