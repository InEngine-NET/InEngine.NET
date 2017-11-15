using System;
namespace InEngine.Core.Queue
{
    public class Message
    {
        public string CommandAssemblyName { get; set; }
        public string CommandClassName { get; set; }
        public string SerializedCommand { get; set; }
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    }
}
