using System.Linq;

namespace IntegrationEngine.Core.Configuration
{
    public class MailConfiguration : IMailConfiguration
    {
        public string IntegrationPointName { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }

        public MailConfiguration()
        {
        }

        public MailConfiguration(IEngineConfiguration engineConfiguration, string integrationPointName)
            : this()
        {
            var config = engineConfiguration.IntegrationPoints.Mail.Single(x => x.IntegrationPointName == integrationPointName);
            IntegrationPointName = integrationPointName;
            HostName = config.HostName;
            Port = config.Port;
        }
    }
}
