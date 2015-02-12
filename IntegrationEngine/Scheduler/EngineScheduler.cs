using Common.Logging;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.MessageQueue;
using IntegrationEngine.Model;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IntegrationEngine.Scheduler
{
    public class EngineScheduler : IEngineScheduler
    {
        public IScheduler Scheduler { get; set; }
        public virtual IList<Type> IntegrationJobTypes { get; set; }
        public IMessageQueueClient MessageQueueClient { get; set; }
        public ILog Log { get; set; }

        public EngineScheduler()
        {
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void Start()
        {
            Scheduler.Start();
        }

        public void AddSchedulerListener(EngineSchedulerListener engineSchedulerListener)
        {
            Scheduler.ListenerManager.AddSchedulerListener(engineSchedulerListener);
        }

        public virtual void Shutdown()
        {
            Scheduler.Shutdown();
        }

        public Type GetRegisteredJobTypeByName(string jobTypeName)
        {
            var jobTypes = IntegrationJobTypes.Where(x => x.FullName == jobTypeName);
            if (jobTypes.Any())
                return jobTypes.Single();
            Log.Warn(x => x("JobType is not registered: {0}", jobTypeName));
            return null;
        }

        public bool IsJobTypeRegistered(string jobTypeName)
        {
            return GetRegisteredJobTypeByName(jobTypeName) != null;
        }

        public IJobDetail JobDetailFactory(Type jobType, IDictionary<string, string> parameters, IIntegrationJobTrigger triggerDefinition)
        {
            try
            {
                var integrationJob = Activator.CreateInstance(jobType) as IIntegrationJob;
                var jobDetailsDataMap = new JobDataMap();
                jobDetailsDataMap.Put("MessageQueueClient", MessageQueueClient);
                jobDetailsDataMap.Put("IntegrationJob", integrationJob);
                jobDetailsDataMap.Put("Parameters", parameters);
                return JobBuilder.Create<IntegrationJobDispatcherJob>()
                    .SetJobData(jobDetailsDataMap)
                    .StoreDurably(true)
                    .WithIdentity(triggerDefinition.Id, jobType.FullName)
                    .Build();
            }
            catch (Exception exception)
            {
                var message = string.Format("Error creating job detail for type: {0}", jobType.FullName);
                Log.Error(x => x(message), exception);
                throw new Exception(message, exception);
            }
        }

        public virtual void ScheduleJobWithTrigger<T>(T item)
            where T : IIntegrationJobTrigger
        {
            var jobType = GetRegisteredJobTypeByName(item.JobType);
            if (jobType == null)
                return;
            var jobDetail = JobDetailFactory(jobType, item.Parameters, item);
            var trigger = TriggerFactory(item, jobType, jobDetail);
            TryScheduleJobWithTrigger(trigger, jobType, jobDetail, item.StateId);
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
                triggersForJobs.Add(TriggerFactory(triggerDef as CronTrigger, jobType, jobDetail));
            Scheduler.ScheduleJob(jobDetail, triggersForJobs, true);
            foreach (var triggerDef in triggerDefs)
                SetTriggerState(TriggerKeyFactory(triggerDef.Id, jobType), triggerDef.StateId);
        }

        TriggerKey TriggerKeyFactory(string name, Type jobType)
        {
            if (name == null)
                throw new ArgumentNullException();
            if (jobType == null)
                throw new ArgumentNullException();
            return new TriggerKey(name, jobType.FullName);
        }

        TriggerBuilder TriggerBuilderFactory(string name, Type jobType, IJobDetail jobDetail)
        {
            return TriggerBuilder.Create().WithIdentity(TriggerKeyFactory(name, jobType)).ForJob(jobDetail);
        }

        public ITrigger TriggerFactory<T>(T item, Type jobType, IJobDetail jobDetail)
            where T : IIntegrationJobTrigger
        {
            var triggerBuilder = TriggerBuilderFactory(item.Id, jobType, jobDetail);
            if (item is CronTrigger) {
                var cronTrigger = item as CronTrigger;
                triggerBuilder.WithCronSchedule(cronTrigger.CronExpressionString, x => x.InTimeZone(TimeZoneInfo.Utc));
            }
            else if (item is SimpleTrigger) {
                var simpleTrigger = item as SimpleTrigger;
                Action<SimpleScheduleBuilder> simpleScheduleBuilderAction;
                if (simpleTrigger.RepeatCount > 0)
                    simpleScheduleBuilderAction = x => x.WithInterval(simpleTrigger.RepeatInterval).WithRepeatCount(simpleTrigger.RepeatCount);
                else
                    simpleScheduleBuilderAction = x => x.WithInterval(simpleTrigger.RepeatInterval);
                triggerBuilder.WithSimpleSchedule(simpleScheduleBuilderAction);
                if (!object.Equals(simpleTrigger.StartTimeUtc, default(DateTimeOffset)))
                    triggerBuilder.StartAt(simpleTrigger.StartTimeUtc);
                else
                    triggerBuilder.StartNow();
            }
            return triggerBuilder.Build();
        }

        public bool DeleteTrigger(IIntegrationJobTrigger triggerDefinition)
        {
            var jobType = GetRegisteredJobTypeByName(triggerDefinition.JobType);
            var triggerKey = TriggerKeyFactory(triggerDefinition.Id, jobType);
            return Scheduler.UnscheduleJob(triggerKey);
        }
    }
}
