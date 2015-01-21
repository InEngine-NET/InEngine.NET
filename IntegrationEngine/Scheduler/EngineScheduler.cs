using Common.Logging;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.MessageQueue;
using IntegrationEngine.Model;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IntegrationEngine.Scheduler
{
    public class EngineScheduler : IEngineScheduler
    {
        public IScheduler Scheduler { get; set; }
        public IList<Type> IntegrationJobTypes { get; set; }
        public IMessageQueueClient MessageQueueClient { get; set; }
        public ILog Log { get; set; }

        public EngineScheduler()
        {
        }

        public void Start()
        {
            var triggerListener = new TriggerListener();
            var mgr = Scheduler.ListenerManager;
            mgr.AddTriggerListener(triggerListener, GroupMatcher<TriggerKey>.AnyGroup());
            Scheduler.Start();
        }

        public void Shutdown()
        {
         //   Scheduler.ListenerManager.RemoveTriggerListener();
            Scheduler.Shutdown();
        }

        public Type GetRegisteredJobTypeByName(string jobTypeName)
        {
            var jobTypes = IntegrationJobTypes.Where(x => x.FullName == jobTypeName);
            return jobTypes.Any() ? jobTypes.Single() : null;
        }

        public bool IsJobTypeRegistered(string jobTypeName)
        {
            return GetRegisteredJobTypeByName(jobTypeName) != null;
        }

        public IJobDetail JobDetailFactory(Type jobType)
        {
            try
            {
                var integrationJob = Activator.CreateInstance(jobType) as IIntegrationJob;
                var jobDetailsDataMap = new JobDataMap();
                jobDetailsDataMap.Put("MessageQueueClient", MessageQueueClient);
                jobDetailsDataMap.Put("IntegrationJob", integrationJob);
                return JobBuilder.Create<IntegrationJobDispatcherJob>()
                    .SetJobData(jobDetailsDataMap)
                    .StoreDurably(true)
                    .WithIdentity(jobType.Name, jobType.Namespace)
                    .Build();
            }
            catch (Exception exception)
            {
                var message = string.Format("Error creating job detail for type: {0}", jobType.FullName);
                Log.Error(x => x(message), exception);
                throw new Exception(message, exception);
            }
        }

        public virtual void ScheduleJobWithCronTrigger(CronTrigger triggerDefinition)
        {
            var jobType = GetRegisteredJobTypeByName(triggerDefinition.JobType);
            var jobDetail = JobDetailFactory(jobType);
            var trigger = CronTriggerFactory(triggerDefinition, jobType, jobDetail);
            TryScheduleJobWithTrigger(trigger, jobType, jobDetail, triggerDefinition.StateId);
        }

        public void ScheduleJobWithSimpleTrigger(SimpleTrigger triggerDefinition)
        {
            var jobType = GetRegisteredJobTypeByName(triggerDefinition.JobType);
            var jobDetail = JobDetailFactory(jobType);
            var trigger = SimpleTriggerFactory(triggerDefinition, jobType, jobDetail);
            TryScheduleJobWithTrigger(trigger, jobType, jobDetail, triggerDefinition.StateId);
        }

        public void TryScheduleJobWithTrigger(ITrigger trigger, Type jobType, IJobDetail jobDetail, int stateId)
        {
            if (Scheduler.CheckExists(jobDetail.Key))
                Scheduler.RescheduleJob(trigger.Key, trigger);
            else
                Scheduler.ScheduleJob(jobDetail, trigger);
            SetTriggerState(trigger.Key, stateId);
        }

        public void SetTriggerState(TriggerKey triggerKey, int triggerState)
        {
            switch ((TriggerState)triggerState)
            {
                case TriggerState.Paused:
                    Scheduler.PauseTrigger(triggerKey);
                    break;
                case TriggerState.Normal:
                    Scheduler.ResumeTrigger(triggerKey);
                    break;
            }
        }

        public void ScheduleJobsWithTriggers(IEnumerable<IIntegrationJobTrigger> triggerDefs, Type jobType, IJobDetail jobDetail)
        {
            if (!triggerDefs.Any())
                return;
            var triggersForJobs = new Quartz.Collection.HashSet<ITrigger>();
            foreach (var triggerDef in triggerDefs)
            {
                if (triggerDef is CronTrigger)
                    triggersForJobs.Add(CronTriggerFactory(triggerDef as CronTrigger, jobType, jobDetail));
                else if (triggerDef is SimpleTrigger)
                    triggersForJobs.Add(SimpleTriggerFactory(triggerDef as SimpleTrigger, jobType, jobDetail));
            }
            Scheduler.ScheduleJob(jobDetail, triggersForJobs, true);
            foreach (var triggerDef in triggerDefs)
                SetTriggerState(TriggerKeyFactory(triggerDef, jobType), triggerDef.StateId);
        }

        TriggerKey TriggerKeyFactory(IIntegrationJobTrigger integrationJobTrigger, Type jobType)
        {
            return new TriggerKey(integrationJobTrigger.Id, jobType.FullName);
        }

        TriggerBuilder TriggerBuilderFactory(IIntegrationJobTrigger integrationJobTrigger, Type jobType)
        {
            return TriggerBuilder.Create().WithIdentity(TriggerKeyFactory(integrationJobTrigger, jobType));
        }

        public ITrigger SimpleTriggerFactory(SimpleTrigger triggerDefinition, Type jobType, IJobDetail jobDetail)
        {
            var triggerBuilder = TriggerBuilderFactory(triggerDefinition, jobType);
            Action<SimpleScheduleBuilder> simpleScheduleBuilderAction;
            if (triggerDefinition.RepeatCount > 0)
                simpleScheduleBuilderAction = x => x.WithInterval(triggerDefinition.RepeatInterval).WithRepeatCount(triggerDefinition.RepeatCount);
            else
                simpleScheduleBuilderAction = x => x.WithInterval(triggerDefinition.RepeatInterval);
            triggerBuilder.WithSimpleSchedule(simpleScheduleBuilderAction);
            if (!object.Equals(triggerDefinition.StartTimeUtc, default(DateTimeOffset)))
                triggerBuilder.StartAt(triggerDefinition.StartTimeUtc);
            else
                triggerBuilder.StartNow();
            return triggerBuilder.Build();
        }

        public ITrigger CronTriggerFactory(CronTrigger triggerDefinition, Type jobType, IJobDetail jobDetail)
        {
            var triggerBuilder = TriggerBuilderFactory(triggerDefinition, jobType);
            triggerBuilder.WithCronSchedule(triggerDefinition.CronExpressionString, x => x.InTimeZone(triggerDefinition.TimeZoneInfo));
            return triggerBuilder.Build();
        }
        
        public bool DeleteTrigger(IIntegrationJobTrigger triggerDefinition)
        {
            try
            {
                var jobType = GetRegisteredJobTypeByName(triggerDefinition.JobType);
                var triggerKey = TriggerKeyFactory(triggerDefinition, jobType);
                return Scheduler.UnscheduleJob(triggerKey);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                return false;
            }
        }
    }
}
