using IntegrationEngine.Core.Storage;
using IntegrationEngine.Model;
using IntegrationEngine.Scheduler;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;

namespace IntegrationEngine.Api.Controllers
{
    public class TriggerControllerBase<T> : ApiController where T: class, IIntegrationJobTrigger
    {
        public IElasticsearchRepository Repository { get; set; }
        public IEngineScheduler EngineScheduler { get; set; }

        public TriggerControllerBase()
        {}


        // GET api/T
        public IEnumerable<T> GetList()
        {
            return Repository.SelectAll<T>();
        }

        // GET api/T/5
        [ResponseType(typeof(T))]
        public IHttpActionResult Get(string id)
        {
            var trigger = Repository.SelectById<T>(id);
            if (trigger == null)
                return NotFound();
            return Ok(trigger);
        }

        // PUT api/T/5
        public IHttpActionResult Put(string id, T trigger)
        {
            if (id != trigger.Id)
                return BadRequest();
            if (!ModelState.IsValid)
                BadRequest(ModelState);
            Repository.Update(trigger);
            EngineScheduler.ScheduleJobWithTrigger(trigger);
            return Ok(Repository.SelectById<T>(trigger.Id));
        }

        // POST api/T
        [ResponseType(typeof(T))]
        public IHttpActionResult Post(T trigger)
        {
            if (!ModelState.IsValid)
                BadRequest(ModelState);
            var triggerWithId = Repository.Insert(trigger);
            EngineScheduler.ScheduleJobWithTrigger(trigger);
            return CreatedAtRoute("DefaultApi", new { id = triggerWithId.Id }, triggerWithId);
        }

        // DELETE api/T/5
        [ResponseType(typeof(T))]
        public IHttpActionResult Delete(string id)
        {
            var trigger = Repository.SelectById<T>(id);
            if (trigger == null)
                return NotFound();
            Repository.Delete<T>(trigger.Id);
            EngineScheduler.DeleteTrigger(trigger);
            return Ok(trigger);
        }
    }
}
