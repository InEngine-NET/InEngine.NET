using Common.Logging;
using IntegrationEngine.Core.Storage;
using Quartz;

namespace IntegrationEngine.Scheduler
{
    public interface IEngineSchedulerListener : ISchedulerListener
    {
        IElasticsearchRepository ElasticsearchRepository { get; set; }
        ILog Log { get; set; }
    }
}
