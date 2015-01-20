using CronExpressionDescriptor;
using IntegrationEngine.Model;
using Nest;
using System;
using QuartzTriggerState = Quartz.TriggerState;
using System.ComponentModel.DataAnnotations;

namespace IntegrationEngine.Scheduler
{
    public class CronTrigger : ICronTrigger, ICronTriggerReadOnly, IHasStateDescription
    {
        public string Id { get; set; }
        [Required]
        [JobType]
        public string JobType { get; set; }
        [Required]
        [CronExpressionString]
        public string CronExpressionString { get; set; }
        [Required]
        [TimeZoneId]
        public string TimeZoneId { get; set; }
        [Range(0, 1)]
        public int StateId { get; set; }
        [ElasticProperty(OptOut = true)]
        public string CronExpressionDescription { get { return ExpressionDescriptor.GetDescription(CronExpressionString); } }
        [ElasticProperty(OptOut = true)]
        public TimeZoneInfo TimeZoneInfo { get { return TimeZoneId.GetTimeZoneInfo(); } }
        [ElasticProperty(OptOut = true)]
        public string StateDescription { get { return StateId.GetStateDescription(); } }
    }
}
