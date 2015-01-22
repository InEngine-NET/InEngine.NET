using IntegrationEngine.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace IntegrationEngine.Scheduler
{
    public class SimpleTrigger : IntegrationEngine.Model.SimpleTrigger
    {
        [Required]
        [JobType]
        public override string JobType { get; set; }
        [Range(0, 1)]
        public override int StateId { get; set; }
    }
}
