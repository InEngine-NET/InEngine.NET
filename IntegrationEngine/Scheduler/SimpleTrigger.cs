using IntegrationEngine.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IntegrationEngine.Scheduler
{
    public class SimpleTrigger : IntegrationEngine.Model.SimpleTrigger, IntegrationEngine.Model.IHasParameters
    {
        [Required]
        public override string JobType { get; set; }
        [Range(0, 1)]
        public override int StateId { get; set; }
        public override IDictionary<string, string> Parameters { get; set; }
    }
}
