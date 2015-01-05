using System;

namespace IntegrationEngine.Models
{
    public class IntegrationJob
    {
        public Int16 Id { get; set; }
        public long IntervalTicks { get; set; }
        public System.DateTimeOffset StartTimeUtc { get; set; }
        public string JobType { get; set; }
    }
}
