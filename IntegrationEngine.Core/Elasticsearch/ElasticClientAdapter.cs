using Nest;
using IntegrationEngine.Core.IntegrationPoint;
using IntegrationEngine.Core.Configuration;

namespace IntegrationEngine.Core.Elasticsearch
{
    public class ElasticClientAdapter : ElasticClient, IIntegrationPoint<IElasticsearchConfiguration>
    {}
}
