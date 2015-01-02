using System;
using IntegrationEngine.Jobs;
using System.Net.Mail;

namespace IntegrationEngine.ConsoleHost.Car
{
    public class CarMailMessageJob : MailMessageJob
    {
        public CarMailMessageJob() : base()
        {
        }

        public override void Run()
        {
            MailMessage.To.Add("ethanhann@gmail.com");
            MailMessage.Subject = "Your car report is ready.";
            MailMessage.From = new MailAddress("root@localhost");
            MailMessage.Body = "Body content about cars.";
            MailClient.Send(MailMessage);
        }
    }
}

