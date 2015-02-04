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
        public virtual int StateId { get; set; }
        public virtual IDictionary<string, string> Parameters { get; set; }
        [ElasticProperty(OptOut = true)]
        public virtual string CronExpressionDescription { get { return CronExpressionString.GetHumanReadableCronSchedule(); } }
        [ElasticProperty(OptOut = true)]
        public virtual string StateDescription { get { return StateId.GetStateDescription(); } }

        public override string ToString()
        {
            return string.Format("[CronTrigger: Id={0}, JobType={1}, CronExpressionString={2}, StateId={3}, Parameters={4}, CronExpressionDescription={5}, StateDescription={6}]", Id, JobType, CronExpressionString, StateId, Parameters, CronExpressionDescription, StateDescription);
        }
    }
}
