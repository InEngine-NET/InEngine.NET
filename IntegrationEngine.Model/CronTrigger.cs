using System;

namespace IntegrationEngine.Model
{
    public class CronTrigger : IHasStringId, IIntegrationJobTrigger
    {
        public string Id { get; set; }
        public string JobType { get; set; }
        public string CronExpressionString { get; set; }
        TimeZoneInfo _timeZone { get; set; }
        public TimeZoneInfo TimeZone { get { return _timeZone ?? TimeZoneInfo.Utc; } set { _timeZone = value; } }
    }
}
