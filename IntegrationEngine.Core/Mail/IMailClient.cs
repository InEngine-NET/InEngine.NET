using IntegrationEngine.Core.Configuration;
using System.Net.Mail;

namespace IntegrationEngine.Core.Mail
{
    public interface IMailClient
    {
        MailConfiguration MailConfiguration { get; set; }
        SmtpClient SmtpClient { get; set; }
        void Send(MailMessage mailMessage);
    }
}

