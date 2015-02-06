using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Model
{
    public interface IRetryOnFailure
    {
        int RetryCount { get; set; }
        TimeSpan RetryInterval { get; set; }
        TimeSpan RetryStartDelay { get; set; }
    }
}
