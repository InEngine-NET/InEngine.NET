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

        // GET api/SimpleTrigger
        public IEnumerable<SimpleTrigger> GetSimpleTriggers()
        {
            return Repository.SelectAll();
        }

        // GET api/SimpleTrigger/5
        [ResponseType(typeof(SimpleTrigger))]
        public IHttpActionResult GetSimpleTrigger(string id)
        {
            var trigger = Repository.SelectById(id);
            if (trigger == null)
                return NotFound();
            return Ok(trigger);
        }

        // PUT api/SimpleTrigger/5
        public IHttpActionResult PutSimpleTrigger(string id, SimpleTrigger trigger)
        {
            if (id != trigger.Id)
                return BadRequest();
            if (ModelState.IsValid)
                BadRequest(ModelState);
            Repository.Update(trigger);
            EngineScheduler.ScheduleJobWithSimpleTrigger(trigger);
            return Ok(Repository.SelectById(trigger.Id));
        }

        // POST api/SimpleTrigger
        [ResponseType(typeof(SimpleTrigger))]
        public IHttpActionResult PostSimpleTrigger(SimpleTrigger trigger)
        {
            if (ModelState.IsValid)
                BadRequest(ModelState);
            var triggerWithId = Repository.Insert(trigger);
            EngineScheduler.ScheduleJobWithSimpleTrigger(triggerWithId);
            return CreatedAtRoute("DefaultApi", new { id = triggerWithId.Id }, triggerWithId);
        }

        // DELETE api/SimpleTrigger/5
        [ResponseType(typeof(SimpleTrigger))]
        public IHttpActionResult DeleteSimpleTrigger(string id)
        {
            var trigger = Repository.SelectById(id);
            if (trigger == null)
                return NotFound();
            Repository.Delete(trigger.Id);
            EngineScheduler.DeleteTrigger(trigger);
            return Ok(trigger);
        }
    }
}
