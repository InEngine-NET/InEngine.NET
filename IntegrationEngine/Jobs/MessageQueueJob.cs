using System;
using Quartz;
using IntegrationEngine.MessageQueue;

namespace IntegrationEngine.Jobs
{
    public class MessageQueueJob : IJob
    {
        public IMessageQueueClient MessageQueueClient { get; set; }

        public MessageQueueJob()
        {
            MessageQueueClient = ContainerSingleton.GetContainer().Resolve<IMessageQueueClient>();
        }

        public virtual void Execute(IJobExecutionContext context)
        {
            var contextDataMap = context.MergedJobDataMap;
            if (contextDataMap.ContainsKey("IntegrationJob"))
                MessageQueueClient.Publish(contextDataMap.Get("IntegrationJob"));
        }
    }
}
