using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Model
{
    public interface ICronTrigger : IIntegrationJobTrigger
    {
        string CronExpressionString { get; set; }
        string CronExpressionDescription { get; }
        string TimeZoneId { get; set; }
        TimeZoneInfo TimeZoneInfo { get; }
    }
}
