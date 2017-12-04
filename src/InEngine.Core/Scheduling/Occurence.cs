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

        public LifeCycleActions RegisterJob(Action<DailyTimeIntervalScheduleBuilder> action)
        {
            return new LifeCycleActions { 
                JobRegistration = Schedule.RegisterJob(Command, JobDetail, MakeTriggerBuilder(Command).WithDailyTimeIntervalSchedule(action).Build()) 
            };
        }

        public LifeCycleActions RegisterJob(string cronExpression)
        {
            return new LifeCycleActions {
                JobRegistration = Schedule.RegisterJob(Command, JobDetail, MakeTriggerBuilder(Command).WithCronSchedule(cronExpression).Build())
            };
        }

        public LifeCycleActions RegisterJob(Action<SimpleScheduleBuilder> action)
        {
            return new LifeCycleActions {
                JobRegistration = Schedule.RegisterJob(Command, JobDetail, MakeTriggerBuilder(Command).WithSimpleSchedule(action).Build())
            };
        }

        public LifeCycleActions Cron(string cronExpression)
        {
            return RegisterJob(cronExpression);
        }

        public LifeCycleActions EverySecond()
        {
            return RegisterJob(x => x.WithIntervalInSeconds(1).RepeatForever());
        }

        public LifeCycleActions EveryMinute()
        {
            return RegisterJob(x => x.WithIntervalInMinutes(1).RepeatForever());
        }

        public LifeCycleActions EveryFiveMinutes()
        {
            return RegisterJob(x => x.WithIntervalInMinutes(5).RepeatForever());
        }

        public LifeCycleActions EveryTenMinutes()
        {
            return RegisterJob(x => x.WithIntervalInMinutes(10).RepeatForever());
        }

        public LifeCycleActions EveryFifteenMinutes()
        {
            return RegisterJob(x => x.WithIntervalInMinutes(15).RepeatForever());
        }

        public LifeCycleActions EveryThirtyMinutes()
        {
            return RegisterJob(x => x.WithIntervalInMinutes(30).RepeatForever());
        }

        public LifeCycleActions Hourly()
        {
            return RegisterJob(x => x.WithIntervalInHours(1).RepeatForever());
        }

        public LifeCycleActions HourlyAt(int minutesAfterTheHour)
        {
            return RegisterJob($"0 {minutesAfterTheHour} * * * ?");
        }

        public LifeCycleActions Daily()
        {
            return RegisterJob(x => x.WithIntervalInHours(24).RepeatForever());
        }

        public LifeCycleActions DailyAt(int hours, int minutes, int seconds = 0)
        {
            return RegisterJob(x => x.StartingDailyAt(new TimeOfDay(hours, minutes, seconds)));
        }
    }
}
