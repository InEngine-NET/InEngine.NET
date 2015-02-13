using System;
using System.Collections.Generic;

namespace IntegrationEngine.Core.Configuration
{
    public class IntegrationPointConfigurations
    {
        public IList<MailConfiguration> Mail { get; set; }
        public IList<RabbitMQConfiguration> RabbitMQ { get; set; }
        public IList<ElasticsearchConfiguration> Elasticsearch { get; set; }
    }
}

