using System;
using Quartz;

namespace InEngine.Core.Scheduling
{
    public class Occurence
    {
        public Schedule Schedule { get; set; }
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

        public LifecycleActions RegisterJob(Action<DailyTimeIntervalScheduleBuilder> action)
        {
            return new LifecycleActions { 
                JobRegistration = Schedule.RegisterJob(Command, JobDetail, MakeTriggerBuilder(Command).WithDailyTimeIntervalSchedule(action).Build()) 
            };
        }

        public LifecycleActions RegisterJob(string cronExpression)
        {
            return new LifecycleActions {
                JobRegistration = Schedule.RegisterJob(Command, JobDetail, MakeTriggerBuilder(Command).WithCronSchedule(cronExpression).Build())
            };
        }

        public LifecycleActions RegisterJob(Action<SimpleScheduleBuilder> action)
        {
            return new LifecycleActions {
                JobRegistration = Schedule.RegisterJob(Command, JobDetail, MakeTriggerBuilder(Command).WithSimpleSchedule(action).Build())
            };
        }

        public LifecycleActions Cron(string cronExpression)
        {
            return RegisterJob(cronExpression);
        }

        public LifecycleActions EverySecond()
        {
            return RegisterJob(x => x.WithIntervalInSeconds(1).RepeatForever());
        }

        public LifecycleActions EveryMinute()
        {
            return RegisterJob(x => x.WithIntervalInMinutes(1).RepeatForever());
        }

        public LifecycleActions EveryFiveMinutes()
        {
            return RegisterJob(x => x.WithIntervalInMinutes(5).RepeatForever());
        }

        public LifecycleActions EveryTenMinutes()
        {
            return RegisterJob(x => x.WithIntervalInMinutes(10).RepeatForever());
        }

        public LifecycleActions EveryFifteenMinutes()
        {
            return RegisterJob(x => x.WithIntervalInMinutes(15).RepeatForever());
        }

        public LifecycleActions EveryThirtyMinutes()
        {
            return RegisterJob(x => x.WithIntervalInMinutes(30).RepeatForever());
        }

        public LifecycleActions Hourly()
        {
            return RegisterJob(x => x.WithIntervalInHours(1).RepeatForever());
        }

        public LifecycleActions HourlyAt(int minutesAfterTheHour)
        {
            return RegisterJob($"0 {minutesAfterTheHour} * * * ?");
        }

        public LifecycleActions Daily()
        {
            return RegisterJob(x => x.WithIntervalInHours(24).RepeatForever());
        }

        public LifecycleActions DailyAt(int hours, int minutes, int seconds = 0)
        {
            return RegisterJob(x => x.StartingDailyAt(new TimeOfDay(hours, minutes, seconds)));
        }
    }
}
