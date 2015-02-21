using IntegrationEngine.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IntegrationEngine.Scheduler
{
    public class CronTrigger : IntegrationEngine.Model.CronTrigger, IntegrationEngine.Model.IHasParameters
    {
        [Required]
        public override string JobType { get; set; }
        [Required]
        [CronExpressionString]
        public override string CronExpressionString { get; set; }
        [Range(0, 1)]
        public override int StateId { get; set; }
        public override IDictionary<string, string> Parameters { get; set; }
    }
}
