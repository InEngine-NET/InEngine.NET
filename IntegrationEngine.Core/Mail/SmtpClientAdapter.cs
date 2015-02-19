using System;
using System.Net.Mail;

namespace IntegrationEngine.Core
{
    public class SmtpClientAdapter : ISmtpClient
    {
        public SmtpClient SmtpClient { get; set; }
        public string HostName { get { return SmtpClient.Host; } set { SmtpClient.Host = value; } }
        public int Port { get { return SmtpClient.Port; } set { SmtpClient.Port = value; } }

        public SmtpClientAdapter()
        {
            SmtpClient = new SmtpClient();
        }
            
        public virtual void Send(MailMessage mailMessage)
        {
            SmtpClient.Send(mailMessage);
        }
    }
}

