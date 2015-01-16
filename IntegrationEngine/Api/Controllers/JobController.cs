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
    public class JobController : ApiController
    {
        public IEngineScheduler EngineScheduler { get; set; }

        public JobController()
        {}

        public JobController(IEngineScheduler engineScheduler)
            : this()
        {
            EngineScheduler = engineScheduler;
        }

        public IHttpActionResult GetJobs()
        {
            return Ok(EngineScheduler.Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()).ToList());
        }

    }
}
