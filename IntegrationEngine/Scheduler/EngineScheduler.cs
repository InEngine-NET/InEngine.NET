using System;
using System.Collections.Generic;
using System.Linq;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Model;
using Quartz;
using IntegrationEngine.MessageQueue;

namespace IntegrationEngine.Scheduler
{
    public class EngineScheduler : IEngineScheduler
    {
        public IScheduler Scheduler { get; set; }
        public IList<Type> IntegrationJobTypes { get; set; }
        public IMessageQueueClient MessageQueueClient { get; set; }

        public EngineScheduler()
        {}

        public void Start()
        {
            Scheduler.Start();
        }

        public IJobDetail CreateJobDetail(Type jobType)
        {
            var integrationJob = Activator.CreateInstance(jobType) as IIntegrationJob;
            var jobDetailsDataMap = new JobDataMap();
            jobDetailsDataMap.Put("MessageQueueClient", MessageQueueClient);
            jobDetailsDataMap.Put("IntegrationJob", integrationJob);
            return JobBuilder.Create<IntegrationJobDispatcherJob>()
                .SetJobData(jobDetailsDataMap)
                .WithIdentity(jobType.Name, jobType.Namespace)
                .Build();
        }

        public void ScheduleJobWithCronTrigger(CronTrigger triggerDefinition)
        {
            var jobType = IntegrationJobTypes.Where(x => x.FullName == triggerDefinition.JobType).First();
            var jobDetail = CreateJobDetail(jobType);
            ScheduleJobWithCronTrigger(triggerDefinition, jobType, jobDetail);
        }

        public void ScheduleJobWithCronTrigger(CronTrigger triggerDefinition, Type jobType, IJobDetail jobDetail)
        {
            var trigger = TriggerBuilder.Create()
                .WithIdentity(GenerateTriggerId(jobType, triggerDefinition), jobType.Namespace);
            if (triggerDefinition.CronExpressionString != null) {
                trigger.WithCronSchedule(triggerDefinition.CronExpressionString, x => x.InTimeZone(triggerDefinition.TimeZone));
            }
            Scheduler.ScheduleJob(jobDetail, trigger.Build());
        }

        public void ScheduleJobWithSimpleTrigger(SimpleTrigger triggerDefinition)
        {
            var jobType = IntegrationJobTypes.Where(x => x.FullName == triggerDefinition.JobType).First();
            var jobDetail = CreateJobDetail(jobType);
            ScheduleJobWithSimpleTrigger(triggerDefinition, jobType, jobDetail);;
        }

        public void ScheduleJobWithSimpleTrigger(SimpleTrigger triggerDefinition, Type jobType, IJobDetail jobDetail)
        {
            var trigger = TriggerBuilder.Create()
                .WithIdentity(GenerateTriggerId(jobType, triggerDefinition), jobType.Namespace);
            Action<SimpleScheduleBuilder> simpleScheduleBuilderAction;
            if (triggerDefinition.RepeatCount > 0)
                simpleScheduleBuilderAction = x => x.WithInterval(triggerDefinition.RepeatInterval).WithRepeatCount(triggerDefinition.RepeatCount);
            else
                simpleScheduleBuilderAction = x => x.WithInterval(triggerDefinition.RepeatInterval);
            trigger.WithSimpleSchedule(simpleScheduleBuilderAction);
            if (!object.Equals(triggerDefinition.StartTimeUtc, default(DateTimeOffset)))
                trigger.StartAt(triggerDefinition.StartTimeUtc);
            else
                trigger.StartNow();
            Scheduler.ScheduleJob(jobDetail, trigger.Build());
        }

        public void ScheduleJobsWithCronTriggers(IEnumerable<CronTrigger> triggers, Type jobType, IJobDetail jobDetail)
        {
            foreach (var triggerDefinition in triggers.Where(x => x.JobType == jobType.FullName))
                ScheduleJobWithCronTrigger(triggerDefinition, jobType, jobDetail);
        }

        public void ScheduleJobsWithSimpleTriggers(IEnumerable<SimpleTrigger> triggers, Type jobType, IJobDetail jobDetail)
        {
            foreach (var triggerDefinition in triggers.Where(x => x.JobType == jobType.FullName))
                ScheduleJobWithSimpleTrigger(triggerDefinition, jobType, jobDetail);
        }

        string GenerateTriggerId(Type jobType, IHasStringId triggerDefinition)
        {
            return string.Format("{0}-{1}", jobType.Name, triggerDefinition.Id);
        }
    }
}
