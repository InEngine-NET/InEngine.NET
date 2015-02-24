using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.MessageQueue;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;

namespace IntegrationEngine.JobProcessor
{
    public class MessageQueueListenerFactory
    {
        public IUnityContainer UnityContainer { get; set; }
        public IList<Type> IntegrationJobTypes { get; set; }
        public IRabbitMQConfiguration RabbitMQConfiguration { get; set; }

        public MessageQueueListenerFactory()
        {
        }

        public MessageQueueListenerFactory(IUnityContainer unityContainer, IList<Type> integrationJobTypes, IRabbitMQConfiguration rabbitMQConfiguration)
            : this()
        {
            UnityContainer = unityContainer;
            IntegrationJobTypes = integrationJobTypes;
            RabbitMQConfiguration = rabbitMQConfiguration;
        }

        public virtual IMessageQueueListener CreateRabbitMQListener()
        {
            return new RabbitMQListener() {
                IntegrationJobTypes = IntegrationJobTypes,
                MessageQueueConnection = new MessageQueueConnection(RabbitMQConfiguration),
                RabbitMQConfiguration = RabbitMQConfiguration,
                UnityContainer = UnityContainer,
            };
        }
    }
}
