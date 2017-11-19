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
            var consume = new Consume();
            foreach (var index in Enumerable.Range(0, 8).ToList()) 
            {
                var primaryQueueConsumer = JobBuilder
                    .Create<Consume>()
                    .WithIdentity(consume.Name + index, "primaryQueueConsumer")
                    .Build();

                var secondaryQueueConsumer = JobBuilder
                    .Create<Consume>()
                    .WithIdentity(consume.Name + index, "secondaryQueueConsumer")
                    .Build();
                secondaryQueueConsumer.JobDataMap.Add("useSecondaryQueue", true);

                var primaryTrigger = TriggerBuilder
                    .Create()
                    .WithIdentity($"{consume.Name}-primary-{index}", "queue")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
                    .Build();

                var secondaryTrigger = TriggerBuilder
                    .Create()
                    .WithIdentity($"{consume.Name}-secondary-{index}", "queue")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
                    .Build();

                scheduler.ScheduleJob(primaryQueueConsumer, primaryTrigger);
                scheduler.ScheduleJob(secondaryQueueConsumer, secondaryTrigger);
            }
        }
    }
}
