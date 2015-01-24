using IntegrationEngine.Model;
using IntegrationEngine.Scheduler;
using InEngineTimeZone = IntegrationEngine.Model.TimeZone;
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

        public IEnumerable<ITimeZone> GetTimeZones()
        {
            return TimeZoneInfo.GetSystemTimeZones().Select(x => new InEngineTimeZone(x));
        }
    }
}
