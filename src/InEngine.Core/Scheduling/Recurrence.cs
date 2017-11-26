using System;
using Quartz;
using Quartz.Impl;

namespace InEngine.Core.Scheduling
{
    public class Recurrence
    {
        public IScheduler Scheduler { get; set; } = StdSchedulerFactory.GetDefaultScheduler();
        public AbstractCommand Command { get; set; }
        public IJobDetail JobDetail { get; set; }

        public TriggerBuilder MakeTriggerBuilder(AbstractCommand command)
        {
            return TriggerBuilder
                .Create()
                .WithIdentity($"{command.Name}:job:{command.ScheduleId}", command.SchedulerGroup);
        }

        public void EverySecond()
        {
            var trigger = MakeTriggerBuilder(Command)
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
                .Build();
            Scheduler.ScheduleJob(JobDetail, trigger);
        }

        public void EveryMinute()
        {
            var trigger = MakeTriggerBuilder(Command)
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever())
                .Build();
            Scheduler.ScheduleJob(JobDetail, trigger);
        }

        public void EveryFiveMinutes()
        {
            var trigger = MakeTriggerBuilder(Command)
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever())
                .Build();
            Scheduler.ScheduleJob(JobDetail, trigger);
        }

        public void EveryTenMinutes()
        {
            var trigger = MakeTriggerBuilder(Command)
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(10).RepeatForever())
                .Build();
            Scheduler.ScheduleJob(JobDetail, trigger);
        }

        public void EveryFifteenMinutes()
        {
            var trigger = MakeTriggerBuilder(Command)
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(15).RepeatForever())
                .Build();
            Scheduler.ScheduleJob(JobDetail, trigger);
        }

        public void EveryThirtyMinutes()
        {
            var trigger = MakeTriggerBuilder(Command)
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(30).RepeatForever())
                .Build();
            Scheduler.ScheduleJob(JobDetail, trigger);
        }

        public void Hourly()
        {
            var trigger = MakeTriggerBuilder(Command)
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever())
                .Build();
            Scheduler.ScheduleJob(JobDetail, trigger);
        }

        public void HourlyAt(int minutesAfterTheHour)
        {
            var trigger = MakeTriggerBuilder(Command)
                .StartNow()
                .WithCronSchedule($"0 {minutesAfterTheHour} * * * ?")
                .Build();
            Scheduler.ScheduleJob(JobDetail, trigger);
        }

        public void Daily()
        {
            var trigger = MakeTriggerBuilder(Command)
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInHours(24).RepeatForever())
                .Build();
            Scheduler.ScheduleJob(JobDetail, trigger);
        }

        public void DailyAt(TimeOfDay timeOfDay)
        {
            var trigger = MakeTriggerBuilder(Command)
                .StartNow()
                .WithDailyTimeIntervalSchedule(x => x.StartingDailyAt(timeOfDay))
                .Build();
            Scheduler.ScheduleJob(JobDetail, trigger);
        }
    }
}
