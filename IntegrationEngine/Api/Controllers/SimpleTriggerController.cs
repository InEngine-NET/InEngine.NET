using IntegrationEngine.Model;
using IntegrationEngine.Core.Storage;
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

namespace IntegrationEngine.Api.Controllers
{
    public class SimpleTriggerController : ApiController
    {
        public IRepository<SimpleTrigger> Repository { get; set; }
        public IEngineScheduler EngineScheduler { get; set; }

        public SimpleTriggerController()
        {}

        public SimpleTriggerController(ESRepository<SimpleTrigger> repository, IEngineScheduler engineScheduler)
            : this()
        {
            Repository = repository;
            EngineScheduler = engineScheduler;
        }

        // GET api/IntegrationJob
        public IEnumerable<SimpleTrigger> GetIntegrationJobs()
        {
            return Repository.SelectAll();
        }

        // GET api/IntegrationJob/5
        [ResponseType(typeof(SimpleTrigger))]
        public IHttpActionResult GetIntegrationJob(string id)
        {
            var trigger = Repository.SelectById(id);
            if (trigger == null)
                return NotFound();
            return Ok(trigger);
        }

        // PUT api/IntegrationJob/5
        public IHttpActionResult PutIntegrationJob(string id, SimpleTrigger trigger)
        {
            if (id != trigger.Id)
                return BadRequest();
            Repository.Update(trigger);
            EngineScheduler.ScheduleJobWithSimpleTrigger(trigger);
            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST api/IntegrationJob
        [ResponseType(typeof(SimpleTrigger))]
        public IHttpActionResult PostIntegrationJob(SimpleTrigger trigger)
        {
            var triggerWithId = Repository.Insert(trigger);
            EngineScheduler.ScheduleJobWithSimpleTrigger(triggerWithId);
            return CreatedAtRoute("DefaultApi", new { id = triggerWithId.Id }, triggerWithId);
        }

        // DELETE api/IntegrationJob/5
        [ResponseType(typeof(SimpleTrigger))]
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
