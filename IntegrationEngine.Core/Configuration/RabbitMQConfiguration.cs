
namespace IntegrationEngine.Core.Configuration
{
    public class RabbitMQConfiguration : IRabbitMQConfiguration
    {
        public string IntegrationPointName { get; set; }
        public string QueueName { get; set; }
        public string ExchangeName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string HostName { get; set; }
        public string VirtualHost { get; set; }
    }
}
