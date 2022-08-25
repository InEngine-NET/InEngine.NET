using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using InEngine.Core.IO;
using Quartz;
using Serialize.Linq.Extensions;

namespace InEngine.Core.Scheduling
{
    public class Schedule : ISchedule
    {
        public IDictionary<string, JobGroup> JobGroups { get; set; } = new Dictionary<string, JobGroup>();
        public MailSettings MailSettings { get; set; }

        public Occurence Command(AbstractCommand command)
        {
            var jobDetail = MakeJobBuilder(command).Build();
            command.MailSettings = command.CommandLifeCycle.MailSettings = MailSettings;
            command.GetType()
                   .GetProperties()
                   .ToList()
                   .Where(x => !x.GetCustomAttributes(typeof(DoNotAutoWireAttribute), true).Any())
                   .ToList()
                   .ForEach(x => jobDetail.JobDataMap.Add(x.Name, x.GetValue(command)));
            return new Occurence() {
                Schedule = this,
                JobDetail = jobDetail,
                Command = command
            };
        }

        public Occurence Command(Expression<Action> expressionAction)
        {
            return Command(new Lambda() { ExpressionNode = expressionAction.ToExpressionNode() });
        }

        public Occurence Command(IList<AbstractCommand> commands)
        {
            return Command(new Chain() { Commands = commands });
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

        public JobBuilder MakeJobBuilder(AbstractCommand command)
        {
            return JobBuilder.Create(command.GetType())
                  .WithIdentity($"{command.Name}:job:{command.ScheduleId}", command.SchedulerGroup);
        }
    }
}
