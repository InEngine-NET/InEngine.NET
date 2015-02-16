using System;
using Microsoft.Practices.Unity;
using Quartz;
using IntegrationEngine.Model;
using System.Collections.Generic;
using IntegrationEngine.Core.MessageQueue;

namespace IntegrationEngine.Scheduler
{
    public class IntegrationJobDispatcherJob : IJob
    {
        public IntegrationJobDispatcherJob()
        {
        }

        public virtual void Execute(IJobExecutionContext context)
        {
            var map = context.MergedJobDataMap;
            if (map.ContainsKey("Dispatcher") && 
                map.ContainsKey("IntegrationJob") && 
                map.ContainsKey("Parameters"))
            {
                var parameters = map.Get("Parameters") as IDictionary<string, string>;
                var dispatcher = map.Get("Dispatcher") as IDispatcher;
                dispatcher.Dispatch(map.Get("IntegrationJob"), parameters);
            }
        }
    }
}
