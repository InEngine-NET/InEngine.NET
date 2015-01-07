using System;
using System.Net.Mail;
using IntegrationEngine.Core.Mail;

namespace IntegrationEngine.Core.Jobs
{
    public interface IMailJob : IIntegrationJob
    {
        IMailClient MailClient { get; set; }
    }
}

