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
        public ESRepository<CronTrigger> ESRepository { get; set; }

        public HealthStatusController()
        {}

        public HealthStatusController(IMailClient mailClient, IMessageQueueClient messageQueueClient, ESRepository<CronTrigger> esRepository)
            : this()
        {
            MailClient = mailClient;
            MessageQueueClient = messageQueueClient;
            ESRepository = esRepository;
        }

        [ResponseType(typeof(InEngineHealthStatus))]
        public IHttpActionResult GetHealthStatus()
        {
            return Ok(new InEngineHealthStatus() {
                IsMailServerAvailable = MailClient.IsServerAvailable(),
                IsMessageQueueServerAvailable = MessageQueueClient.IsServerAvailable(),
                IsElasticsearchServerAvailable = ESRepository.IsServerAvailable(),
            });
        }
    }
}
