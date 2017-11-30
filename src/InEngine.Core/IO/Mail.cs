using System;
using System.Net.Mail;

namespace InEngine.Core.IO
{
    public class Mail
    {
        public string Host { get; set; }
        public int Port { get; set; }

        public void Send(string fromAddress, string toAddress, string subject, string body)
        {
            new SmtpClient() {
                Host = Host,
                Port = Port
            }.Send(new MailMessage(fromAddress, toAddress, subject, body));
        }
    }
}
