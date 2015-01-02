using IntegrationEngine.Models;
using IntegrationEngine.Storage;
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
    public class MailMessageJobController : ApiController
    {
        public Repository<MailMessageJob> Repository { get; set; } 
        public MailMessageJobController()
        {
            Repository = Container.Resolve<Repository<MailMessageJob>>();
        }

        // GET api/MailMessageJob
        public IEnumerable<MailMessageJob> GetMailMessageJobs()
        {
            return Repository.SelectAll();
        }

        // GET api/MailMessageJob/5
        [ResponseType(typeof(MailMessageJob))]
        public IHttpActionResult GetMailMessageJob(int id)
        {
            var mailMessageJob = Repository.SelectById(id);
            if (mailMessageJob == null)
                return NotFound();
            return Ok(mailMessageJob);
        }

        // PUT api/MailMessageJob/5
        public IHttpActionResult PutMailMessageJob(int id, MailMessageJob mailMessageJob)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != mailMessageJob.Id)
                return BadRequest();

            Repository.db.Entry(mailMessageJob).State = EntityState.Modified;

            try
            {
                Repository.Save();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MailMessageJobExists(id))
                    return NotFound();
                else
                    throw;
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST api/MailMessageJob
        [ResponseType(typeof(MailMessageJob))]
        public IHttpActionResult PostMailMessageJob(MailMessageJob mailMessageJob)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            Repository.Insert(mailMessageJob);
            Repository.Save();
            return CreatedAtRoute("DefaultApi", new { id = mailMessageJob.Id }, mailMessageJob);
        }

        // DELETE api/MailMessageJob/5
        [ResponseType(typeof(MailMessageJob))]
        public IHttpActionResult DeleteMailMessageJob(int id)
        {
            var mailMessageJob = Repository.SelectById(id);
            if (mailMessageJob == null)
                return NotFound();
            Repository.Delete(mailMessageJob);
            Repository.Save();
            return Ok(mailMessageJob);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Repository.db.Dispose();
            base.Dispose(disposing);
        }

        private bool MailMessageJobExists(int id)
        {
            return Repository.Exists(id);
        }
    }
}
