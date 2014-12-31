using System;
using System.Net.Mail;

namespace IntegrationEngine.Mail
{
    public interface IMailClient
    {
        MailConfiguration MailConfiguration { get; set; }
        SmtpClient SmtpClient { get; set; }
        void Send(MailMessage mailMessage);
    }
}

