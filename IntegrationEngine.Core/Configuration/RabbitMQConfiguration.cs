using System.Linq;

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

        public RabbitMQConfiguration()
        {
        }

        public RabbitMQConfiguration(IEngineConfiguration engineConfiguration, string integrationPointName)
            : this()
        {
            var config = engineConfiguration.IntegrationPoints.RabbitMQ.Single(x => x.IntegrationPointName == integrationPointName);
            IntegrationPointName = integrationPointName;
            QueueName = config.QueueName;
            ExchangeName = config.ExchangeName;
            UserName = config.UserName;
            Password = config.Password;
            HostName = config.HostName;
            VirtualHost = config.VirtualHost;
        }
    }
}
