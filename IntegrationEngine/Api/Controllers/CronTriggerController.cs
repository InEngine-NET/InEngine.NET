using IntegrationEngine.Core.Storage;
using IntegrationEngine.Model;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;

namespace IntegrationEngine.Api.Controllers
{
    public class CronTriggerController : ApiController
    {
        public IRepository<CronTrigger> Repository { get; set; }
        public IEngineScheduler EngineScheduler { get; set; }

        public CronTriggerController()
        {
            Repository = Container.TryResolve<ESRepository<CronTrigger>>();
            EngineScheduler = Container.TryResolve<IEngineScheduler>();
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
            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST api/IntegrationJob
        [ResponseType(typeof(CronTrigger))]
        public IHttpActionResult PostIntegrationJob(CronTrigger trigger)
        {
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
