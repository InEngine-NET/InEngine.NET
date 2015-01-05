using System;
using Quartz;

namespace IntegrationEngine.MessageQueue
{
    public class IntegrationJobDispatcherJob : IJob
    {
        public IMessageQueueClient MessageQueueClient { get; set; }

        public IntegrationJobDispatcherJob()
        {
            MessageQueueClient = Container.Resolve<IMessageQueueClient>();
        }

        public virtual void Execute(IJobExecutionContext context)
        {
            var contextDataMap = context.MergedJobDataMap;
            if (contextDataMap.ContainsKey("IntegrationJob"))
                MessageQueueClient.Publish(contextDataMap.Get("IntegrationJob"));
        }
    }
}
