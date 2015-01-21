using Common.Logging;
using IntegrationEngine.Core.Configuration;
using System;
using System.Net.Mail;
using System.Net.Sockets;
using System.IO;

namespace IntegrationEngine.Core.Mail
{
    public class MailClient : IMailClient
    {
        public SmtpClient SmtpClient { get; set; }
        public MailConfiguration MailConfiguration { get; set; }
        public ILog Log { get; set; }

        public MailClient()
        {}

        public MailClient (ILog log) : this()
        {
            Log = log;
        }

        public void Send(MailMessage mailMessage)
        {
            ConfigureSmtpClient();
            try
            {
                SmtpClient.Send(mailMessage);
            } 
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        void ConfigureSmtpClient()
        {
            if (SmtpClient == null)
                SmtpClient = new SmtpClient();
            SmtpClient.Host = MailConfiguration.HostName;
            SmtpClient.Port = MailConfiguration.Port;
        }

        public bool IsServerAvailable()
        {
            try 
            {
                using (var client = new TcpClient())
                {
                    client.Connect(MailConfiguration.HostName, MailConfiguration.Port);
                    using (var stream = client.GetStream())
                    {
                        using (var writer = new StreamWriter(stream))
                        using (var reader = new StreamReader(stream))
                        {
                            writer.WriteLine("EHLO " + MailConfiguration.HostName);
                            writer.Flush();
                            var response = reader.ReadLine();
                            Log.Debug(x => x("Mail server status: {0}", response));
                            if (response != null)
                                return true;
                        }
                    }
                }
            }
            catch(SocketException exception) 
            {
                Log.Error(exception);
                return false;
            }
            return false;
        }
    }
}

