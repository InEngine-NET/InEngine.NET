using System;
using InEngine.Core;
using Quartz;
using NLog;


namespace InEngineScheduler
{
    public static class Jobs
    {
        public static void Schedule(IScheduler scheduler)
        {
            var logger = LogManager.GetCurrentClassLogger();
            Plugin.Discover<IJobs>().ForEach(x => {
                logger.Info($"Registering jobs from plugin: {x.Name}");
                x.Make<IJobs>().ForEach(y => y.Schedule(scheduler));
            });
        }
    }
}
