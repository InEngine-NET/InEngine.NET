using System;
using System.Net.Mail;
using IntegrationEngine.Mail;

namespace IntegrationEngine.Jobs
{
    public class MailMessageJob : IMailMessageJob
    {
        public TimeSpan Interval { get; set; }
        public DateTimeOffset StartTimeUtc { get; set; }
        public MailMessage MailMessage { get; set; }
        public IMailClient MailClient { get; set; }

        public MailMessageJob()
        {
            MailClient = Container.Resolve<IMailClient>();
            MailMessage = new MailMessage();
        }

        public virtual void Run()
        {
            MailClient.Send(MailMessage);
        }
    }
}

