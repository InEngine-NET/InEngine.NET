using IntegrationEngine.Scheduler;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.MessageQueue;
using InEngineHealthStatus = IntegrationEngine.Model.HealthStatus;
using Nest;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;


namespace IntegrationEngine.Api.Controllers
{
    public class HealthStatusController : ApiController
    {
        public IMailClient MailClient { get; set; }
        public IMessageQueueClient MessageQueueClient { get; set; }
        public IElasticsearchRepository Repository { get; set; }

        public HealthStatusController()
        {}

        public HealthStatusController(IMailClient mailClient, IMessageQueueClient messageQueueClient, IElasticsearchRepository repository)
            : this()
        {
            MailClient = mailClient;
            MessageQueueClient = messageQueueClient;
            Repository = repository;
        }

        [ResponseType(typeof(InEngineHealthStatus))]
        public IHttpActionResult GetHealthStatus()
        {
            return Ok(new InEngineHealthStatus() {
                IsMailServerAvailable = MailClient.IsServerAvailable(),
                IsMessageQueueServerAvailable = MessageQueueClient.IsServerAvailable(),
                IsElasticsearchServerAvailable = Repository.IsServerAvailable(),
            });
        }
    }
}
