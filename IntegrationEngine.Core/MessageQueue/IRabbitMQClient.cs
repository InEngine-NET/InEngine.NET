using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.IntegrationPoint;

namespace IntegrationEngine.Core.MessageQueue
{
    public interface IRabbitMQClient : IMessageQueueClient, IIntegrationPoint<IMailConfiguration>
    {}
}
