using IntegrationEngine.Scheduler;
using InEngineTimeZone = IntegrationEngine.Scheduler.TimeZone;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;

namespace IntegrationEngine.Api.Controllers
{
    public class TimeZoneController : ApiController
    {
        public TimeZoneController()
        {}

        [ResponseType(typeof(ITimeZone))]
        public IHttpActionResult GetTimeZones()
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones().Select(x => new InEngineTimeZone(x)).ToList();
            return Ok(timeZones);
        }
    }
}
