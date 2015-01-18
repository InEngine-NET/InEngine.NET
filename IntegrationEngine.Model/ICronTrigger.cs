using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Model
{
    public interface ICronTrigger : IHasStringId, IIntegrationJobTrigger
    {
        string CronExpressionString { get; set; }
        string TimeZoneId { get; set; }
        TimeZoneInfo TimeZone { get; set; }
    }
}
