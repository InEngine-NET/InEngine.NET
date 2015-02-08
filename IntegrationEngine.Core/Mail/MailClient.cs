using Common.Logging;
using IntegrationEngine.Core.Points;
using System;
using System.IO;
using System.Net.Mail;
using System.Net.Sockets;
using System.Reflection;

namespace IntegrationEngine.Core.Mail
{
    public class MailClient : IMailClient, IMailPoint
    {
        public ISmtpClient SmtpClient { get; set; }
        public ILog Log { get; set; }
        public string HostName { get { return SmtpClient.HostName; } set { SmtpClient.HostName = value; } }
        public int Port { get { return SmtpClient.Port; } set { SmtpClient.Port = value; } }

        public MailClient()
        {
            SmtpClient = new SmtpClientAdapter();
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
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
                    client.Connect(HostName, Port);
                    using (var stream = client.GetStream())
                    {
                        using (var writer = new StreamWriter(stream))
                        using (var reader = new StreamReader(stream))
                        {
                            writer.WriteLine("EHLO " + HostName);
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

