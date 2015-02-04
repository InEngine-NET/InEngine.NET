using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Scheduler;
using IntegrationEngine.Api;

namespace IntegrationEngine.Api.Controllers
{
    public class CronTriggerController : TriggerControllerBase<CronTrigger>
    {
        public CronTriggerController() : base() {}

        public CronTriggerController(IElasticsearchRepository repository, IEngineScheduler engineScheduler)
            : base()
        {
            Repository = repository;
            EngineScheduler = engineScheduler;
        }
    }
}
