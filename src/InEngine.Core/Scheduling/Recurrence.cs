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

        public ITrigger MakeTrigger(AbstractCommand command, Action<DailyTimeIntervalScheduleBuilder> action)
        {
            return MakeTriggerBuilder(command)
                .WithDailyTimeIntervalSchedule(action)
                .Build();
        }

        public ITrigger MakeTrigger(AbstractCommand command, string cronExpression)
        {
            return MakeTriggerBuilder(command)
                .WithCronSchedule(cronExpression)
                .Build();
        }

        public ITrigger MakeTrigger(AbstractCommand command, Action<SimpleScheduleBuilder> action)
        {
            return MakeTriggerBuilder(command)
                .WithSimpleSchedule(action)
                .Build();
        }

        public void EverySecond()
        {
            Scheduler.ScheduleJob(
                JobDetail,
                MakeTrigger(Command, x => x.WithIntervalInSeconds(1).RepeatForever())
            );
        }

        public void EveryMinute()
        {
            Scheduler.ScheduleJob(
                JobDetail,
                MakeTrigger(Command, x => x.WithIntervalInMinutes(1).RepeatForever())
            );
        }

        public void EveryFiveMinutes()
        {
            Scheduler.ScheduleJob(
                JobDetail,
                MakeTrigger(Command, x => x.WithIntervalInMinutes(5).RepeatForever())
            );
        }

        public void EveryTenMinutes()
        {
            Scheduler.ScheduleJob(
                JobDetail,
                MakeTrigger(Command, x => x.WithIntervalInMinutes(10).RepeatForever())
            );
        }

        public void EveryFifteenMinutes()
        {
            Scheduler.ScheduleJob(
                JobDetail,
                MakeTrigger(Command, x => x.WithIntervalInMinutes(15).RepeatForever())
            );
        }

        public void EveryThirtyMinutes()
        {
            Scheduler.ScheduleJob(
                JobDetail,
                MakeTrigger(Command, x => x.WithIntervalInMinutes(30).RepeatForever())
            );
        }

        public void Hourly()
        {
            Scheduler.ScheduleJob(
                JobDetail,
                MakeTrigger(Command, x => x.WithIntervalInHours(1).RepeatForever())
            );
        }

        public void HourlyAt(int minutesAfterTheHour)
        {
            Scheduler.ScheduleJob(
                JobDetail,
                MakeTrigger(Command, $"0 {minutesAfterTheHour} * * * ?")
            );
        }

        public void Daily()
        {
            Scheduler.ScheduleJob(
                JobDetail,
                MakeTrigger(Command, x => x.WithIntervalInHours(24).RepeatForever())
            );
        }

        public void DailyAt(TimeOfDay timeOfDay)
        {
            Scheduler.ScheduleJob(
                JobDetail, 
                MakeTrigger(Command, x => x.StartingDailyAt(timeOfDay))
            );
        }
    }
}
