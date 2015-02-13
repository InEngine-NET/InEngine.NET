namespace IntegrationEngine.Core.Configuration
{
    public class MailConfiguration : IMailConfiguration
    {
        public string IntegrationPointName { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
    }
}
