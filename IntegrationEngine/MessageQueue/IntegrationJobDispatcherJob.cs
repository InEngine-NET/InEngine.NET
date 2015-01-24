using System;
using Microsoft.Practices.Unity;
using Quartz;
using IntegrationEngine.Model;
using System.Collections.Generic;

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
            if (map.ContainsKey("MessageQueueClient") && 
                map.ContainsKey("IntegrationJob") && 
                map.ContainsKey("Parameters"))
            {
                var messageQueueClient = map.Get("MessageQueueClient") as IMessageQueueClient;
                var parameters = map.Get("Parameters") as IDictionary<string, string>;
                messageQueueClient.Publish(map.Get("IntegrationJob"), parameters);
            }
        }
    }
}
