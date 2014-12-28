using System;
using System.Text;
using Funq;
using Quartz;
using RabbitMQ.Client;
using IntegrationEngine.MessageQueue;

namespace IntegrationEngine.Jobs
{
    abstract public class AsyncJob : IJob
    {
        public IMessageQueueClient MessageQueueClient { get; set; }

        public AsyncJob()
        {
            MessageQueueClient = ContainerSingleton.GetContainer().Resolve<IMessageQueueClient>();
        }

        public virtual void Execute(IJobExecutionContext context)
        {
        }
    }
}
