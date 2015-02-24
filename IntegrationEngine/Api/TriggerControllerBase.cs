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
                return BadRequest(ModelState);
            if (!IsValidJobType(trigger.JobType))
                return BadRequest(string.Format("JobType is not registered: {0}", trigger.JobType));
            var updatedTrigger = Repository.Update(trigger);
            EngineScheduler.ScheduleJobWithTrigger(updatedTrigger);
            return Ok(updatedTrigger);
        }

        // POST api/T
        [ResponseType(typeof(IIntegrationJobTrigger))]
        public IHttpActionResult Post(T trigger)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (!IsValidJobType(trigger.JobType))
                return BadRequest(string.Format("JobType is not registered: {0}", trigger.JobType));
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

        /// <summary>
        /// Determines whether this instance is valid JobType given a jobTypeName.
        /// </summary>
        /// <returns><c>true</c> if the jobTypeName is the name of a registered JobType; otherwise, <c>false</c>.
        /// Also, return <c>true</c> if the jobTypeName is an empty string, because returning <c>false</c>
        /// would mean that the JobType is required which is an additional constraint - it is not
        /// the responsibility of this method to enforce that constraint.
        /// </returns>
        /// <param name="jobTypeName">The fully qualified name of a JobType.</param>
        public bool IsValidJobType(string jobTypeName)
        {
            if (string.IsNullOrEmpty(jobTypeName))
                return true;
            return EngineScheduler.IsJobTypeRegistered(jobTypeName);
        }
    }
}
