using System;
using IntegrationEngine.Jobs;
using System.Net.Mail;
using IntegrationEngine.Mail;

namespace IntegrationEngine.ConsoleHost.Car
{
    public class CarMailMessageJob : IMailJob
    {
        public IMailClient MailClient { get; set; }
        public TimeSpan Interval { get; set; }
        public DateTimeOffset StartTimeUtc { get; set; }

        public void Run()
        {
            var mailMessage = new MailMessage();
            mailMessage.To.Add("ethanhann@gmail.com");
            mailMessage.Subject = "Your car report is ready.";
            mailMessage.From = new MailAddress("root@localhost");
            mailMessage.Body = "Body content about cars.";
            MailClient.Send(mailMessage);
        }
    }
}

