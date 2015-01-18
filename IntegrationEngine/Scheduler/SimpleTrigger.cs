using IntegrationEngine.Model;
using System;

namespace IntegrationEngine.Scheduler
{
    public class SimpleTrigger : ISimpleTrigger
    {
        public string Id { get; set; }
        public string JobType { get; set; }
        public int RepeatCount { get; set; }
        public TimeSpan RepeatInterval { get; set; }
        public DateTimeOffset StartTimeUtc { get; set; }
    }
}
