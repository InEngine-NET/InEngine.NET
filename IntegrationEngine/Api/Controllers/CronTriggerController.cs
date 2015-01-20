using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Scheduler;

namespace IntegrationEngine.Api.Controllers
{
    public class CronTriggerController : ApiController
    {
        public ESRepository<CronTrigger> Repository { get; set; }
        public IEngineScheduler EngineScheduler { get; set; }

        public CronTriggerController()
        {}

        public CronTriggerController(ESRepository<CronTrigger> repository, IEngineScheduler engineScheduler)
            : this()
        {
            Repository = repository;
            EngineScheduler = engineScheduler;
        }

        // GET api/CronTrigger
        public IEnumerable<CronTrigger> GetCronTriggers()
        {
            return Repository.SelectAll();
        }

        // GET api/CronTrigger/5
        [ResponseType(typeof(CronTrigger))]
        public IHttpActionResult GetCronTrigger(string id)
        {
            var trigger = Repository.SelectById(id);
            if (trigger == null)
                return NotFound();
            return Ok(trigger);
        }

        // PUT api/CronTrigger/5
        public IHttpActionResult PutCronTrigger(string id, CronTrigger trigger)
        {
            if (id != trigger.Id)
                return BadRequest();
            if (ModelState.IsValid)
                BadRequest(ModelState);
            Repository.Update(trigger);
            EngineScheduler.ScheduleJobWithCronTrigger(trigger);
            return Ok(Repository.SelectById(trigger.Id));
        }

        // POST api/CronTrigger
        [ResponseType(typeof(CronTrigger))]
        public IHttpActionResult PostCronTrigger(CronTrigger trigger)
        {
            if (ModelState.IsValid)
                BadRequest(ModelState);
            var triggerWithId = Repository.Insert(trigger);
            EngineScheduler.ScheduleJobWithCronTrigger(triggerWithId);
            return CreatedAtRoute("DefaultApi", new { id = triggerWithId.Id }, triggerWithId);
        }

        // DELETE api/CronTrigger/5
        [ResponseType(typeof(CronTrigger))]
        public IHttpActionResult DeleteCronTrigger(string id)
        {
            var trigger = Repository.SelectById(id);
            if (trigger == null)
                return NotFound();
            Repository.Delete(trigger.Id);
            return Ok(trigger);
        }
    }
}
