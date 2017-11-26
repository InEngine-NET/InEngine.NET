using System;
namespace InEngine.Core.Queuing
{
    public class Message
    {
        public string CommandAssemblyName { get; set; }
        public string CommandClassName { get; set; }
        public string SerializedCommand { get; set; }
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
        public bool IsCompressed { get; set; }
    }
}
