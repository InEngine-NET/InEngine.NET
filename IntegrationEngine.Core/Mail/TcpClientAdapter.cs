using System;
using System.IO;
using System.Net.Sockets;

namespace IntegrationEngine.Core.Mail
{
    public class TcpClientAdapter : ITcpClient
    {
        private TcpClient TcpClient;

        public TcpClientAdapter()
        {
            TcpClient = new TcpClient();
        }

        public virtual void Connect(string hostname, int port)
        {
            TcpClient.Connect(hostname, port);
        }

        public virtual Stream GetStream()
        {
            return TcpClient.GetStream();
        }

        public virtual void Close()
        {
            TcpClient.Close();
        }
    }
}

