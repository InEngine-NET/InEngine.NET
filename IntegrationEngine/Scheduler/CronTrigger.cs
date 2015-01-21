using IntegrationEngine.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace IntegrationEngine.Scheduler
{
    public class CronTrigger : IntegrationEngine.Model.CronTrigger
    {
        [Required]
        [JobType]
        public override string JobType { get; set; }
        [Required]
        [CronExpressionString]
        public override string CronExpressionString { get; set; }
        [Required]
        [TimeZoneId]
        public override string TimeZoneId { get; set; }
        [Range(0, 1)]
        public override int StateId { get; set; }
    }
}
