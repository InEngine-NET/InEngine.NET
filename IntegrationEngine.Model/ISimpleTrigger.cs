using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Model
{
    public interface ISimpleTrigger : IIntegrationJobTrigger
    {
        int RepeatCount { get; set; }
        TimeSpan RepeatInterval { get; set; }
        DateTimeOffset StartTimeUtc { get; set; }
    }
}
