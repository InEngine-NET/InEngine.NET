using System;
using Quartz;
using Quartz.Impl;

namespace InEngine.Core.Scheduling
{
    public class Occurence
    {
        public IScheduler Scheduler { get; set; } = StdSchedulerFactory.GetDefaultScheduler();
        public AbstractCommand Command { get; set; }
        public IJobDetail JobDetail { get; set; }

        public static TriggerBuilder MakeTriggerBuilder(AbstractCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(command.GetType().Name, "The command to schedule cannot be null.");
            return TriggerBuilder
                .Create()
                .WithIdentity($"{command.Name}:job:{command.ScheduleId}", command.SchedulerGroup);            
        }

        public void RegisterJob(Action<DailyTimeIntervalScheduleBuilder> action)
        {
            Scheduler.ScheduleJob(JobDetail, MakeTriggerBuilder(Command).WithDailyTimeIntervalSchedule(action).Build());
        }

        public void RegisterJob(string cronExpression)
        {
            Scheduler.ScheduleJob(JobDetail, MakeTriggerBuilder(Command).WithCronSchedule(cronExpression).Build());
        }

        public void RegisterJob(Action<SimpleScheduleBuilder> action)
        {
            Scheduler.ScheduleJob(JobDetail, MakeTriggerBuilder(Command).WithSimpleSchedule(action).Build());
        }

        public void Cron(string cronExpression)
        {
            RegisterJob(cronExpression);
        }

        public void EverySecond()
        {
            RegisterJob(x => x.WithIntervalInSeconds(1).RepeatForever());
        }

        public void EveryMinute()
        {
            RegisterJob(x => x.WithIntervalInMinutes(1).RepeatForever());
        }

        public void EveryFiveMinutes()
        {
            RegisterJob(x => x.WithIntervalInMinutes(5).RepeatForever());
        }

        public void EveryTenMinutes()
        {
            RegisterJob(x => x.WithIntervalInMinutes(10).RepeatForever());
        }

        public void EveryFifteenMinutes()
        {
            RegisterJob(x => x.WithIntervalInMinutes(15).RepeatForever());
        }

        public void EveryThirtyMinutes()
        {
            RegisterJob(x => x.WithIntervalInMinutes(30).RepeatForever());
        }

        public void Hourly()
        {
            RegisterJob(x => x.WithIntervalInHours(1).RepeatForever());
        }

        public void HourlyAt(int minutesAfterTheHour)
        {
            RegisterJob($"0 {minutesAfterTheHour} * * * ?");
        }

        public void Daily()
        {
            RegisterJob(x => x.WithIntervalInHours(24).RepeatForever());
        }

        public void DailyAt(int hours, int minutes, int seconds = 0)
        {
            RegisterJob(x => x.StartingDailyAt(new TimeOfDay(hours, minutes, seconds)));
        }
    }
}
