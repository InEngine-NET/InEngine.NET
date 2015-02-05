using Common.Logging;
using IntegrationEngine.Core.Storage;
using Quartz;
using System;

namespace IntegrationEngine.Scheduler
{
    public class EngineSchedulerListener : IEngineSchedulerListener
    {
        public IElasticsearchRepository ElasticsearchRepository { get; set; }
        public ILog Log { get; set; }

        public void JobAdded(IJobDetail jobDetail)
        {
        }

        public void JobDeleted(JobKey jobKey)
        {
        }

        public void JobPaused(JobKey jobKey)
        {
        }

        public void JobResumed(JobKey jobKey)
        {
        }

        public void JobScheduled(ITrigger trigger)
        {
        }

        public void JobUnscheduled(TriggerKey triggerKey)
        {
        }

        public void JobsPaused(string jobGroup)
        {
        }

        public void JobsResumed(string jobGroup)
        {
        }

        public void SchedulerError(string msg, SchedulerException cause)
        {
        }

        public void SchedulerInStandbyMode()
        {
        }

        public void SchedulerShutdown()
        {
        }

        public void SchedulerShuttingdown()
        {
        }

        public void SchedulerStarted()
        {
        }

        public void SchedulerStarting()
        {
        }

        public void SchedulingDataCleared()
        {
        }

        public void TriggerFinalized(ITrigger trigger)
        {
            try
            {
                if (trigger is ISimpleTrigger)
                    ElasticsearchRepository.Delete<SimpleTrigger>(trigger.Key.Name);       
            }
            catch (Exception exception)
            {
                Log.Error("Error deleting simple trigger.", exception);
            }
        }

        public void TriggerPaused(TriggerKey triggerKey)
        {
        }

        public void TriggerResumed(TriggerKey triggerKey)
        {
        }

        public void TriggersPaused(string triggerGroup)
        {
        }

        public void TriggersResumed(string triggerGroup)
        {
        }
    }
}
