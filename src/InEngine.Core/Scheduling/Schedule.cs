using System;
using System.Collections.Generic;
using System.Linq;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using Quartz;
using Quartz.Impl;

namespace InEngine.Core.Scheduling
{
    public class Schedule
    {
        public IScheduler Scheduler { get; set; } = StdSchedulerFactory.GetDefaultScheduler();
        public IDictionary<string, JobGroup> JobGroups { get; set; } = new Dictionary<string, JobGroup>();

        public Occurence Job(AbstractCommand command)
        {
            var jobDetail = MakeJobBuilder(command).Build();

            command.GetType()
                   .GetProperties()
                   .ToList()
                   .Where(x => !x.GetCustomAttributes(typeof(DoNotAutoWireAttribute), true).Any())
                   .ToList()
                   .ForEach(x => jobDetail.JobDataMap.Add(x.Name, x.GetValue(command)));

            return new Occurence()
            {
                Schedule = this,
                JobDetail = jobDetail,
                Command = command
            };
        }

        public Occurence Job(Action action)
        {
            return Job(new Lambda() { Action = action });
        }

        public Occurence Job(IList<AbstractCommand> commands)
        {
            return Job(new Chain() { Commands = commands });
        }

        public JobRegistration RegisterJob(AbstractCommand command, IJobDetail jobDetail, ITrigger trigger)
        {
            if (!JobGroups.ContainsKey(command.SchedulerGroup))
                JobGroups.Add(command.SchedulerGroup, new JobGroup());
                
            if (JobGroups[command.SchedulerGroup].Registrations.ContainsKey(command.ScheduleId))
                throw new DuplicateScheduledCommandException(command.Name, command.ScheduleId, command.SchedulerGroup);

            var registration = new JobRegistration(command, jobDetail, trigger);
            JobGroups[command.SchedulerGroup].Registrations.Add(command.ScheduleId, registration);
            return registration;
        }

        public void Initialize()
        {
            Plugin.Load<IJobs>().ForEach(x => {
                x.Make<IJobs>().ForEach(y => y.Schedule(this));
            });

            JobGroups.AsEnumerable().ToList().ForEach(x => {
                x.Value.Registrations.AsEnumerable().ToList().ForEach(y => {
                    Scheduler.ScheduleJob(y.Value.JobDetail, y.Value.Trigger);
                });
            });
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
