using System;
using System.Net.Mail;
using IntegrationEngine.Mail;

namespace IntegrationEngine.Jobs
{
    public interface IMailJob : IIntegrationJob
    {
        IMailClient MailClient { get; set; }
    }
}

