using CronExpressionDescriptor;
using Nest;
using System;

namespace IntegrationEngine.Model
{
    public class CronTrigger : ICronTrigger
    {
        public virtual string Id { get; set; }
        public virtual string JobType { get; set; }
        public virtual string CronExpressionString { get; set; }
        public virtual string TimeZoneId { get; set; }
        public virtual int StateId { get; set; }
        [ElasticProperty(OptOut = true)]
        public virtual string CronExpressionDescription { get { return CronExpressionString.GetHumanReadableCronSchedule(); } }
        [ElasticProperty(OptOut = true)]
        public virtual TimeZoneInfo TimeZoneInfo { get { return TimeZoneId.GetTimeZoneInfo(); } }
        [ElasticProperty(OptOut = true)]
        public virtual string StateDescription { get { return StateId.GetStateDescription(); } }
    }
}
