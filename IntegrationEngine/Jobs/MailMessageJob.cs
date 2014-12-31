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
            MailClient = ContainerSingleton.GetContainer().Resolve<IMailClient>();
            MailMessage = new MailMessage();
        }

        public void Run()
        {
            MailMessage.To.Add("ethanhann@gmail.com");
            MailMessage.Subject = "This is the Subject line";
            MailMessage.From = new System.Net.Mail.MailAddress("root@localhost");
            MailMessage.Body = "This is the message body";
            MailClient.Send(MailMessage);
        }
    }
}

