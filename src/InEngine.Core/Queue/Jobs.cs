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
            foreach (var index in Enumerable.Range(0, 16).ToList()) 
            {
                var defaultQueueConsumer = JobBuilder
                    .Create<Consume>()
                    .WithIdentity(consume.Name + index, "defaultQueueConsumer")
                    .Build();

                var expressQueueConsumer = JobBuilder
                    .Create<Consume>()
                    .WithIdentity(consume.Name + index, "expressQueueConsumer")
                    .Build();


                var defaultTrigger = TriggerBuilder
                    .Create()
                    .WithIdentity($"{consume.Name}-default-{index}", "queue")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
                    .Build();
                
                var expressTrigger = TriggerBuilder
                    .Create()
                    .WithIdentity($"{consume.Name}-express-{index}", "queue")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
                    .Build();

                scheduler.ScheduleJob(defaultQueueConsumer, defaultTrigger);
                scheduler.ScheduleJob(expressQueueConsumer, expressTrigger);
            }
        }
    }
}
