using CronExpressionDescriptor;
using Nest;
using CronTriggerModel = IntegrationEngine.Model.CronTrigger;

namespace IntegrationEngine.Scheduler
{
    public class CronTrigger : CronTriggerModel
    {
         [ElasticProperty(OptOut = true)]
        public string CronExpressionDescription { get { return ExpressionDescriptor.GetDescription(CronExpressionString); } }
    }
}
