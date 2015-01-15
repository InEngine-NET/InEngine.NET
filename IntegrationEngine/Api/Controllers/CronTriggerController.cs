using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Model;
using IntegrationEngine.Scheduler;

namespace IntegrationEngine.Api.Controllers
{
    public class CronTriggerController : ApiController
    {
        public ESRepository<CronTrigger> Repository { get; set; }
        public IEngineScheduler EngineScheduler { get; set; }

        public CronTriggerController()
        {
        }

        public CronTriggerController(ESRepository<CronTrigger> repository, IEngineScheduler engineScheduler)
            : this()
        {
            Repository = repository;
            EngineScheduler = engineScheduler;
        }

        // GET api/IntegrationJob
        public IEnumerable<CronTrigger> GetIntegrationJobs()
        {
            return Repository.SelectAll();
        }

        // GET api/IntegrationJob/5
        [ResponseType(typeof(CronTrigger))]
        public IHttpActionResult GetIntegrationJob(string id)
        {
            var trigger = Repository.SelectById(id);
            if (trigger == null)
                return NotFound();
            return Ok(trigger);
        }

        // PUT api/IntegrationJob/5
        public IHttpActionResult PutIntegrationJob(string id, CronTrigger trigger)
        {
            if (id != trigger.Id)
                return BadRequest();
            Repository.Update(trigger);
            EngineScheduler.ScheduleJobWithCronTrigger(trigger);
            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST api/IntegrationJob
        [ResponseType(typeof(CronTrigger))]
        public IHttpActionResult PostIntegrationJob(CronTrigger trigger)
        {
            if (trigger.CronExpressionString.IsValidCronExpression())
                return BadRequest("Cron expression is not valid: " + trigger.CronExpressionString);
            var triggerWithId = Repository.Insert(trigger);
            EngineScheduler.ScheduleJobWithCronTrigger(triggerWithId);
            return CreatedAtRoute("DefaultApi", new { id = triggerWithId.Id }, triggerWithId);
        }

        // DELETE api/IntegrationJob/5
        [ResponseType(typeof(CronTrigger))]
        public IHttpActionResult DeleteIntegrationJob(string id)
        {
            var trigger = Repository.SelectById(id);
            if (trigger == null)
                return NotFound();
            Repository.Delete(trigger.Id);
            return Ok(trigger);
        }
    }
}
