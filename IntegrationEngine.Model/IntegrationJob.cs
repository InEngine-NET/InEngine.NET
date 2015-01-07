using System;

namespace IntegrationEngine.Model
{
    public class IntegrationJob : IHasStringId
    {
        public string Id { get; set; }
        public long IntervalTicks { get; set; }
        public DateTimeOffset StartTimeUtc { get; set; }
        public string JobType { get; set; }
    }
}
