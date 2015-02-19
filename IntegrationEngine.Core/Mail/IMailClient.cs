using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.IntegrationPoint;
using System.Net.Mail;

namespace IntegrationEngine.Core.Mail
{
    public interface IMailClient : IIntegrationPoint<IMailConfiguration>
    {
        ISmtpClient SmtpClient { get; set; }
        void Send(MailMessage mailMessage);
        bool IsServerAvailable();
    }
}

