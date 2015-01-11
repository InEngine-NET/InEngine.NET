using System;

namespace IntegrationEngine.Model
{
    public class SimpleTrigger : IHasStringId, IIntegrationJobTrigger
    {
        public string Id { get; set; }
        public string JobType { get; set; }
        public int RepeatCount { get; set; }
        public TimeSpan RepeatInterval { get; set; }
        public DateTimeOffset StartTimeUtc { get; set; }
    }
}
