using IntegrationEngine.Core.Points;

namespace IntegrationEngine.Core.Configuration
{
    public class MailConfiguration : IMailPoint
    {
        public string HostName { get; set; }
        public int Port { get; set; }
    }
}
