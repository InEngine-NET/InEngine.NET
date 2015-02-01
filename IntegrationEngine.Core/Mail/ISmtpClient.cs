using System;
using System.Net.Mail;

namespace IntegrationEngine.Core
{
    public interface ISmtpClient
    {
        string Host { get; set; }
        int Port { get; set; }
        void Send(MailMessage message);
    }
}

