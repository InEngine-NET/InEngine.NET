using System;
using System.Linq;
using Quartz;
using Quartz.Impl;

namespace InEngine.Core.Scheduling
{
    public class Schedule
    {
        public IScheduler Scheduler { get; set; } = StdSchedulerFactory.GetDefaultScheduler();

        public Recurrence Job(AbstractCommand command)
        {
            var jobDetail = MakeJobBuilder(command).Build();

            command.GetType()
                   .GetProperties()
                   .ToList()
                   .ForEach(x => jobDetail.JobDataMap.Add(x.Name, x.GetValue(command)));

            return new Recurrence() {
                JobDetail = jobDetail,
                Command = command
            };
        }

        public void Start()
        {
            Scheduler.Start();
        }

        public void Shutdown()
        {
            if (Scheduler.IsStarted)
                Scheduler.Shutdown();
        }

        public JobBuilder MakeJobBuilder(AbstractCommand command)
        {
            return JobBuilder
                .Create(command.GetType())
                .WithIdentity($"{command.Name}:job:{command.ScheduleId}", command.SchedulerGroup);
        }
    }
}
