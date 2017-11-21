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
            ScheduleQueueConsumerJobs(scheduler);
            ScheduleQueueConsumerJobs(scheduler, true);
            //foreach (var index in Enumerable.Range(0, 8).ToList()) 
            //{
            //    var consume = new Consume();
            //    consume.Name = consume.Name + index; 
            //    var primaryQueueConsumer = consume.MakeJobBuilder().Build();
            //    var secondaryQueueConsumer = consume.MakeJobBuilder().Build();
            //    secondaryQueueConsumer.JobDataMap.Add("useSecondaryQueue", true);

            //    var primaryTrigger = consume
            //        .MakeTriggerBuilder()
            //        .StartNow()
            //        .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
            //        .Build();

            //    var secondaryTrigger = consume
            //        .MakeTriggerBuilder()
            //        .StartNow()
            //        .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
            //        .Build();

            //    scheduler.ScheduleJob(primaryQueueConsumer, primaryTrigger);
            //    scheduler.ScheduleJob(secondaryQueueConsumer, secondaryTrigger);
            //}
        }

        private void ScheduleQueueConsumerJobs(IScheduler scheduler, bool useSecondaryQueue = false)
        {
            foreach (var index in Enumerable.Range(0, 8).ToList())
            {
                var consume = new Consume() {
                    ScheduleId = $"{(useSecondaryQueue ? "secondary" : "primary")}:{index.ToString()}"
                };
                var job = consume.MakeJobBuilder().Build();
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
