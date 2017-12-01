using MailKit.Net.Smtp;
using MimeKit;

namespace InEngine.Core.IO
{
    public class Mail
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public void Send(string fromAddress, string toAddress, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress));
            message.To.Add(new MailboxAddress(toAddress));
            message.Subject = subject;
            message.Body = new TextPart("plain") { 
                Text = body
            };

            using (var client = new SmtpClient())
            {
                client.Connect(Host, Port, false);
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                if (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
                    client.Authenticate(Username, Password);
                client.Send(message);
                client.Disconnect(true);
            }
        }
    }
}
