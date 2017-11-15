using System;
using Quartz;
using InEngine.Core.Queue.Commands;

namespace InEngine.Core.Queue
{
    public static class Jobs
    {
        public static void Schedule(IScheduler scheduler)
        {
            var consume = new Consume();
            var jobDetails = JobBuilder
                .Create<Consume>()
                .WithIdentity(consume.Name, "queue")
                .Build();

            var trigger = TriggerBuilder
                .Create()
                .WithIdentity(consume.Name, "queue")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
                .Build();

            scheduler.ScheduleJob(jobDetails, trigger);
        }
    }
}
