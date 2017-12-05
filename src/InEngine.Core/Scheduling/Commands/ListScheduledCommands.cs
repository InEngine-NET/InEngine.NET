using System;
using System.Linq;
using System.Collections.Generic;
using Quartz;
using Quartz.Impl.Matchers;

namespace InEngine.Core.Scheduling
{
    public class ListScheduledCommands : AbstractCommand
    {
        public override void Run()
        {
            var schedule = new Schedule();
            schedule.Initialize();

            var scheduler = schedule.Scheduler;
            foreach(var groupName in scheduler.GetJobGroupNames())
            {
                Warning($"Group Name: {groupName}").Newline();
                var groupMatcher = GroupMatcher<JobKey>.GroupContains(groupName);
                var jobKeys = scheduler.GetJobKeys(groupMatcher);
                jobKeys.ToList().ForEach(jobKey => {
                    InfoText("Schedule ID:".PadRight(15)).Line(jobKey.Name);
                    var detail = scheduler.GetJobDetail(jobKey);
                    InfoText("Command:".PadRight(15)).Line(detail.JobType.ToString());
                    scheduler.GetTriggersOfJob(jobKey).ToList().ForEach(trigger => {
                        InfoText("Trigger Name:".PadRight(15)).Line(trigger.Key.Name);
                        InfoText("Trigger Type:".PadRight(15)).Line(trigger.GetType().Name);
                        InfoText("Trigger State".PadRight(15)).Line(scheduler.GetTriggerState(trigger.Key));

                        var nextFireTime = trigger.GetNextFireTimeUtc();
                        if (nextFireTime.HasValue)
                            InfoText("Will Run At:".PadRight(15)).Line(nextFireTime.Value.LocalDateTime.ToString());

                        var previousFireTime = trigger.GetPreviousFireTimeUtc();
                        if (previousFireTime.HasValue)
                            InfoText("Ran At:".PadRight(15)).Line(previousFireTime.Value.LocalDateTime.ToString());
                    });
                    Newline();
                });
            }
        }
    }
}
