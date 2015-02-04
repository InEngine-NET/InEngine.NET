using System;
using System.IO;

namespace IntegrationEngine.Core.Mail
{
    public interface ITcpClient
    {
        void Connect(string hostname, int port);
        Stream GetStream();
        void Close();
    }
}

