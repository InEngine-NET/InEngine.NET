using InEngine.Core;
using InEngine.Core.Scheduling;
using NLog;

namespace InEngine
{
    public static class Jobs
    {
        public static void Schedule(Schedule schedule)
        {
            var logger = LogManager.GetCurrentClassLogger();
            Plugin.Load<IJobs>().ForEach(x => {
                logger.Info($"Registering jobs from plugin: {x.Name}");
                x.Make<IJobs>().ForEach(y => y.Schedule(schedule));
            });
        }
    }
}
