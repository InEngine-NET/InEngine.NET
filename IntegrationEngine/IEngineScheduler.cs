using IntegrationEngine.Model;
using Quartz;
using System;

namespace IntegrationEngine
{
    public interface IEngineScheduler
    {
        IScheduler Scheduler { get; set; }
        void Start();
        void ScheduleJobWithCronTrigger(CronTrigger triggerDefinition);
        void ScheduleJobWithCronTrigger(CronTrigger triggerDefinition, Type jobType, IJobDetail jobDetail);
        void ScheduleJobWithSimpleTrigger(SimpleTrigger triggerDefinition);
        void ScheduleJobWithSimpleTrigger(SimpleTrigger triggerDefinition, Type jobType, IJobDetail jobDetail);
    }
}
