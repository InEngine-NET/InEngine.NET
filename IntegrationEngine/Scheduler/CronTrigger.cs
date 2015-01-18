using CronExpressionDescriptor;
using IntegrationEngine.Model;
using Nest;
using System;

namespace IntegrationEngine.Scheduler
{
    public class CronTrigger : ICronTrigger, ICronTriggerReadOnly
    {
        public string Id { get; set; }
        public string JobType { get; set; }
        public string CronExpressionString { get; set; }
        public string TimeZoneId { get; set; }
        public TimeZoneInfo TimeZone { get; set; }

        [ElasticProperty(OptOut = true)]
        public string CronExpressionDescription { get { return ExpressionDescriptor.GetDescription(CronExpressionString); } }
    }
}
