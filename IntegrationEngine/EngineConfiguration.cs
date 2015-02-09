using IntegrationEngine.Core.Configuration;
using FX.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;

namespace IntegrationEngine
{
    public class EngineConfiguration : JsonConfiguration, IEngineConfiguration
    {
        public EngineConfiguration() : base("IntegrationEngine.json")
        {}

        public WebApiConfiguration WebApi { get; set; }
        public RabbitMQConfiguration MessageQueue { get; set; }
        public ElasticsearchConfiguration Elasticsearch { get; set; }
        public NLogAdapterConfiguration NLogAdapter { get; set; }
        public IntegrationPointConfigurations IntegrationPoints { get; set; }
    }
}
