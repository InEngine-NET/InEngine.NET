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
using System;

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

        public IEnumerable<JobType> GetJobTypes()
        {
            return EngineScheduler.IntegrationJobTypes.Select(x => new JobType() { FullName = x.FullName, Name = x.Name });
        }
    }
}
