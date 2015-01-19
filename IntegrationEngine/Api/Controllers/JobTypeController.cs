using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Model;
using IntegrationEngine.Scheduler;
using Quartz.Impl.Matchers;
using Quartz;

namespace IntegrationEngine.Api.Controllers
{
    public class JobTypeController : ApiController
    {
        public IEngineScheduler EngineScheduler { get; set; }

        public JobTypeController()
        {}

        public JobTypeController(IEngineScheduler engineScheduler)
            : this()
        {
            EngineScheduler = engineScheduler;
        }

        public IHttpActionResult GetJobTypes()
        {
            return Ok(EngineScheduler.Scheduler
                .GetJobKeys(GroupMatcher<JobKey>.AnyGroup())
                .Select(x => x.ToString()).ToList());
        }
    }
}
