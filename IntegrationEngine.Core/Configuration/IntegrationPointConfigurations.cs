using System;
using System.Collections.Generic;

namespace IntegrationEngine.Core.Configuration
{
    public class IntegrationPointConfigurations
    {
        public List<MailConfiguration> Mail { get; set; }
        public List<RabbitMQConfiguration> RabbitMQ { get; set; }
        public List<ElasticsearchConfiguration> Elasticsearch { get; set; }
    }
}

