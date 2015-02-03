using System;
using Nest;

namespace IntegrationEngine.Model
{
    public class LogEvent : IHasStringId
    {
        public string Id { get; set; }
        [ElasticProperty(Name = "@timestamp")]
        public DateTimeOffset Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
    }
}
