using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Jobs
{
    public interface IElasticsearchJob
    {
        IElasticClient ElasticClient { get; set; }
    }
}
