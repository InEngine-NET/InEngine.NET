using System;
using System.Net.Mail;
using IntegrationEngine.Mail;

namespace IntegrationEngine
{
    public interface IMailMessageJob : IIntegrationJob
    {
        MailMessage MailMessage { get; set; }
        IMailClient MailClient { get; set; }
    }
}

