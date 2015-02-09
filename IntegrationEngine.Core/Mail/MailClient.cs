using Common.Logging;
using IntegrationEngine.Core.Configuration;
using System;
using System.IO;
using System.Net.Mail;
using System.Net.Sockets;
using System.Reflection;

namespace IntegrationEngine.Core.Mail
{
    public class MailClient : IMailClient
    {
        public ISmtpClient SmtpClient { get; set; }
        public ILog Log { get; set; }
        public IMailConfiguration MailConfiguration { get; set; }

        public MailClient()
        {
            SmtpClient = new SmtpClientAdapter();
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
            
        public MailClient(IMailConfiguration mailConfiguration) 
            : this()
        {
            MailConfiguration = mailConfiguration;
        }


        public MailClient (ILog log) : this()
        {
            Log = log;
        }

        public void Send(MailMessage mailMessage)
        {
            try
            {
                SmtpClient.Send(mailMessage);
            } 
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        public bool IsServerAvailable()
        {
            var isAvailable = false;
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
                            if (response != null)
                                isAvailable = true;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                isAvailable = false;
            }
            return isAvailable;
        }
    }
}

