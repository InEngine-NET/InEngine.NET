using CronExpressionDescriptor;
using IntegrationEngine.Model;
using Nest;
using System;
using QuartzTriggerState = Quartz.TriggerState;

namespace IntegrationEngine.Scheduler
{
    public class CronTrigger : ICronTrigger, ICronTriggerReadOnly, IHasStateDescription
    {
        public string Id { get; set; }
        public string JobType { get; set; }
        public string CronExpressionString { get; set; }
        public string TimeZoneId { get; set; }
        public int StateId { get; set; }
        [ElasticProperty(OptOut = true)]
        public string CronExpressionDescription { get { return ExpressionDescriptor.GetDescription(CronExpressionString); } }
        [ElasticProperty(OptOut = true)]
        public TimeZoneInfo TimeZoneInfo { get { return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId); } }
        [ElasticProperty(OptOut = true)]
        public string StateDescription { get { return StateId.GetStateDescription(); } }
    }
}
