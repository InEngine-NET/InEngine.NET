using System.Collections.Specialized;
using System.Linq;
using Quartz;
using Quartz.Impl;

namespace InEngine.Core.Scheduling
{
    public class SuperScheduler
    {
        public Schedule Schedule { get; set; }
        public IScheduler Scheduler { get; set; }
        public string SchedulerInstanceName { get; set; } = "InEngine";
        public string SchedulerThreadPoolType { get; set; } = "Quartz.Simpl.SimpleThreadPool, Quartz";
        public string SchedulerThreadCount { get; set; } = "20";
        public string SchedulerThreadPriority { get; set; } = "Normal";

        public void Initialize()
        {
            Scheduler = new StdSchedulerFactory(new NameValueCollection {
                ["quartz.scheduler.instanceName"] = SchedulerInstanceName,
                ["quartz.threadPool.type"] = SchedulerThreadPoolType,
                ["quartz.threadPool.threadCount"] = SchedulerThreadCount,
                ["quartz.threadPool.threadPriority"] = SchedulerThreadPriority
            }).GetScheduler();

            Schedule = new Schedule();
            PluginAssembly.Load<IPlugin>().ForEach(x => {
                x.Plugins.ForEach(y => y.Schedule(Schedule));
            });

            Schedule.JobGroups.AsEnumerable().ToList().ForEach(x => {
                x.Value.Registrations.AsEnumerable().ToList().ForEach(y => {
                    Scheduler.ScheduleJob(y.Value.JobDetail, y.Value.Trigger);
                });
            });
        }

        public void Start()
        {
            Scheduler.Start();
        }

        public void Shutdown()
        {
            if (Scheduler.IsStarted)
                Scheduler.Shutdown();
        }

        public JobBuilder MakeJobBuilder(AbstractCommand command)
        {
            return JobBuilder.Create(command.GetType())
                  .WithIdentity($"{command.Name}:job:{command.ScheduleId}", command.SchedulerGroup);
        }
    }
}
