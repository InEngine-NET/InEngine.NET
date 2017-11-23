using System;
using Quartz;
using InEngine.Core.Queue.Commands;
using System.Collections.Generic;
using System.Linq;

namespace InEngine.Core.Queue
{
    public class Jobs : IJobs
    {
        public void Schedule(IScheduler scheduler)
        {
            var queueSettings = InEngineSettings.Make().Queue;
            ScheduleQueueConsumerJobs(scheduler, queueSettings.PrimaryQueueConsumers);
            ScheduleQueueConsumerJobs(scheduler, queueSettings.SecondaryQueueConsumers, true);
        }

        private void ScheduleQueueConsumerJobs(IScheduler scheduler, int consumers, bool useSecondaryQueue = false)
        {
            if (consumers < 0) {
                throw new ArgumentOutOfRangeException(nameof(consumers), consumers, "The number of queue consumers must be 0 or greater.");
            }
            foreach (var index in Enumerable.Range(0, consumers).ToList())
            {
                var consume = new Consume() {
                    ScheduleId = $"{(useSecondaryQueue ? "secondary" : "primary")}:{index.ToString()}"
                };
                var job = consume.MakeJobBuilder().RequestRecovery(true).Build();
                job.JobDataMap.Add("useSecondaryQueue", useSecondaryQueue);

                var trigger = consume
                    .MakeTriggerBuilder()
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
                    .Build();

                scheduler.ScheduleJob(job, trigger);
            }
        }
    }
}
