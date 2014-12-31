using System;
using System.Net.Mail;

namespace IntegrationEngine.Mail
{
    public class MailClient : IMailClient
    {
        public SmtpClient SmtpClient { get; set; }
        public MailConfiguration MailConfiguration { get; set; }

        public MailClient ()
        {
        }

        public void Send(MailMessage mailMessage)
        {
            ConfigureSmtpClient();
            SmtpClient.Send(mailMessage);
        }

        void ConfigureSmtpClient()
        {
            if (SmtpClient == null)
                SmtpClient = new SmtpClient();
            SmtpClient.Host = MailConfiguration.Host;
            SmtpClient.Port = MailConfiguration.Port;
        }
    }
}

