using IntegrationEngine.Core.Storage;
using IntegrationEngine.Model;
using IntegrationEngine.Scheduler;
using Nest;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;

namespace IntegrationEngine.Api.Controllers
{
    public class LogEventController : ApiController
    {
        public IElasticsearchRepository Repository { get; set; }

        public LogEventController()
        {}

        public LogEventController(IElasticsearchRepository repository)
            : this()
        {
            Repository = repository;
        }

        public IEnumerable<LogEvent> GetAll()
        {
            return Repository.SelectAll<LogEvent, DateTimeOffset>(x => x.Timestamp);
        }
    }
}
