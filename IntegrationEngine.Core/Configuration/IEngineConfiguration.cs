using IntegrationEngine.Core.Configuration;
using System.Collections.Generic;

namespace IntegrationEngine.Core.Configuration
{
    public interface IEngineConfiguration
    {
        WebApiConfiguration WebApi { get; set; }
        RabbitMQConfiguration MessageQueue { get; set; }
        ElasticsearchConfiguration Elasticsearch { get; set; }
        NLogAdapterConfiguration NLogAdapter { get; set; }
//        IList<IntegrationPointConfigurations> IntegrationPoints { get; set; }
        IntegrationPointConfigurations IntegrationPoints { get; set; }
//        IList<string> MailConfigurations { get; set; }
//        IMailConfiguration GetMailConfigurationByName(string integrationPointName);
    }
}
