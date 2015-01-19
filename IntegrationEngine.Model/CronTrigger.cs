using System;

namespace IntegrationEngine.Model
{
    public class CronTrigger : ICronTrigger
    {
        public string Id { get; set; }
        public string JobType { get; set; }
        public string CronExpressionString { get; set; }
        public string TimeZoneId { get; set; }
        public int StateId { get; set; }
    }
}
