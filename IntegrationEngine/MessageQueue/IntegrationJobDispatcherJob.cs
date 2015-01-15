using System;
using Microsoft.Practices.Unity;
using Quartz;

namespace IntegrationEngine.MessageQueue
{
    public class IntegrationJobDispatcherJob : IJob
    {

        public IntegrationJobDispatcherJob()
        {
        }

        public virtual void Execute(IJobExecutionContext context)
        {
            var map = context.MergedJobDataMap;
            if (map.ContainsKey("MessageQueueClient") && map.ContainsKey("IntegrationJob"))
            {
                var messageQueueClient = map.Get("MessageQueueClient") as IMessageQueueClient;
                messageQueueClient.Publish(map.Get("IntegrationJob"));
            }
        }
    }
}
