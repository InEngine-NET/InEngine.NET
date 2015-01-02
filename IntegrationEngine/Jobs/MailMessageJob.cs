using System;
using System.Net.Mail;
using IntegrationEngine.Mail;

namespace IntegrationEngine.Jobs
{
    public partial class MailMessageJob
    {
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

