using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Scheduler;

namespace IntegrationEngine.Api.Controllers
{
    public class SimpleTriggerController : TriggerControllerBase<SimpleTrigger>
    {
        public SimpleTriggerController() : base() {}

        public SimpleTriggerController(IElasticsearchRepository repository, IEngineScheduler engineScheduler)
            : base()
        {
            Repository = repository;
            EngineScheduler = engineScheduler;
        }
    }
}
