using Nest;
using System;
using System.Collections.Generic;

namespace IntegrationEngine.Model
{
    public class CronTrigger : ICronTrigger
    {
        public virtual string Id { get; set; }
        public virtual string JobType { get; set; }
        public virtual string CronExpressionString { get; set; }
        public virtual string TimeZoneId { get; set; }
        public virtual int StateId { get; set; }
        public virtual IDictionary<string, string> Parameters { get; set; }
        [ElasticProperty(OptOut = true)]
        public virtual string CronExpressionDescription { get { return CronExpressionString.GetHumanReadableCronSchedule(); } }
        [ElasticProperty(OptOut = true)]
        public virtual TimeZoneInfo TimeZoneInfo { get { return TimeZoneId.GetTimeZoneInfo(); } }
        [ElasticProperty(OptOut = true)]
        public virtual string StateDescription { get { return StateId.GetStateDescription(); } }

        public override string ToString()
        {
            return string.Format("[CronTrigger: Id={0}, JobType={1}, CronExpressionString={2}, TimeZoneId={3}, StateId={4}, CronExpressionDescription={5}, TimeZoneInfo={6}, StateDescription={7}]", Id, JobType, CronExpressionString, TimeZoneId, StateId, CronExpressionDescription, TimeZoneInfo, StateDescription);
        }
    }
}
