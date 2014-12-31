using System;
using System.Net.Mail;
using log4net;

namespace IntegrationEngine.Mail
{
    public class MailClient : IMailClient
    {
        public SmtpClient SmtpClient { get; set; }
        public MailConfiguration MailConfiguration { get; set; }
        public ILog Log { get; set; }

        public MailClient ()
        {
            Log = Container.Resolve<ILog>();
        }

        public void Send(MailMessage mailMessage)
        {
            ConfigureSmtpClient();
            try {

                SmtpClient.Send(mailMessage);
            } catch (Exception exception) {
                Log.Error("Cannot send mail message", exception);
            }
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

