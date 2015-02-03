using IntegrationEngine.Core.Storage;
using IntegrationEngine.Model;
using IntegrationEngine.Scheduler;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;

namespace IntegrationEngine.Api
{
    public class TriggerControllerBase<T> : ApiController where T: class, IIntegrationJobTrigger
    {
        public IElasticsearchRepository Repository { get; set; }
        public IEngineScheduler EngineScheduler { get; set; }

        public TriggerControllerBase()
        {}


        // GET api/T
        public IEnumerable<T> GetCollection()
        {
            return Repository.SelectAll<T>();
        }

        // GET api/T/5
        [ResponseType(typeof(IIntegrationJobTrigger))]
        public IHttpActionResult Get(string id)
        {
            var trigger = Repository.SelectById<T>(id);
            if (trigger == null)
                return NotFound();
            return Ok(trigger);
        }

        // PUT api/T/5
        [ResponseType(typeof(IIntegrationJobTrigger))]
        public IHttpActionResult Put(string id, T trigger)
        {
            if (id != trigger.Id)
                return BadRequest();
            if (!ModelState.IsValid)
                BadRequest(ModelState);
            var updatedTrigger = Repository.Update(trigger);
            EngineScheduler.ScheduleJobWithTrigger(updatedTrigger);
            return Ok(updatedTrigger);
        }

        // POST api/T
        [ResponseType(typeof(IIntegrationJobTrigger))]
        public IHttpActionResult Post(T trigger)
        {
            if (!ModelState.IsValid)
                BadRequest(ModelState);
            var triggerWithId = Repository.Insert(trigger);
            EngineScheduler.ScheduleJobWithTrigger(triggerWithId);
            return CreatedAtRoute("DefaultApi", new { id = triggerWithId.Id }, triggerWithId);
        }

        // DELETE api/T/5
        [ResponseType(typeof(IIntegrationJobTrigger))]
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
