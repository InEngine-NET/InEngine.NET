using IntegrationEngine.Model;
using Quartz;
using System;
using System.Collections.Generic;

namespace IntegrationEngine.Scheduler
{
    public interface IEngineScheduler
    {
        IList<Type> IntegrationJobTypes { get; set; }
        IScheduler Scheduler { get; set; }
        void Start();
        void ScheduleJobWithCronTrigger(CronTrigger triggerDefinition);
        void ScheduleJobWithSimpleTrigger(SimpleTrigger triggerDefinition);
        bool IsJobTypeRegistered(string jobTypeName);
        Type GetRegisteredJobTypeByName(string jobTypeName);
        bool DeleteTrigger(IIntegrationJobTrigger triggerDefinition);
        void Shutdown();
    }
}
