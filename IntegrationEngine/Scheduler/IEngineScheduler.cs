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
        void ScheduleJobWithTrigger<T>(T item) where T : IIntegrationJobTrigger;
        bool IsJobTypeRegistered(string jobTypeName);
        Type GetRegisteredJobTypeByName(string jobTypeName);
        bool DeleteTrigger(IIntegrationJobTrigger triggerDefinition);
        void Shutdown();
    }
}
