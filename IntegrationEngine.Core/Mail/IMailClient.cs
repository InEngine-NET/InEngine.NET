using IntegrationEngine.Core.Configuration;
using System.Net.Mail;

namespace IntegrationEngine.Core.Mail
{
    public interface IMailClient
    {
        ISmtpClient SmtpClient { get; set; }
        void Send(MailMessage mailMessage);
        bool IsServerAvailable();
    }
}

