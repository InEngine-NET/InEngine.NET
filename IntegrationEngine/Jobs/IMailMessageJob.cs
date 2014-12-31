using System;
using System.Net.Mail;
using IntegrationEngine.Mail;

namespace IntegrationEngine.Jobs
{
    public interface IMailMessageJob : IIntegrationJob
    {
        MailMessage MailMessage { get; set; }
        IMailClient MailClient { get; set; }
    }
}

