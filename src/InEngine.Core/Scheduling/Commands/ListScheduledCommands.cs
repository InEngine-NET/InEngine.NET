﻿using System.Linq;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl.Matchers;

namespace InEngine.Core.Scheduling.Commands;

using System.Globalization;

public class ListScheduledCommands : AbstractCommand
{
    public override async Task RunAsync()
    {
        var superScheduler = new SuperScheduler();
        superScheduler.Initialize();

        var scheduler = superScheduler.Scheduler;
        var jobGroupNames = await scheduler.GetJobGroupNames();

        foreach (var groupName in jobGroupNames)
        {
            Warning($"Group Name: {groupName}").Newline();
            var groupMatcher = GroupMatcher<JobKey>.GroupContains(groupName);
            var jobKeys = await scheduler.GetJobKeys(groupMatcher);
            jobKeys.ToList().ForEach(jobKey =>
            {
                InfoText("Schedule ID:".PadRight(15)).Line(jobKey.Name);
                var detail = scheduler.GetJobDetail(jobKey).Result;
                InfoText("Command:".PadRight(15)).Line(detail?.JobType.ToString() ?? "Unknown");
                var triggers = scheduler.GetTriggersOfJob(jobKey).Result;
                triggers.ToList().ForEach(trigger =>
                {
                    InfoText("Trigger Name:".PadRight(15)).Line(trigger.Key.Name);
                    InfoText("Trigger Type:".PadRight(15)).Line(trigger.GetType().Name);
                    InfoText("Trigger State".PadRight(15)).Line(scheduler.GetTriggerState(trigger.Key));

                    var nextFireTime = trigger.GetNextFireTimeUtc();
                    if (nextFireTime.HasValue)
                        InfoText("Will Run At:".PadRight(15))
                            .Line(nextFireTime.Value.LocalDateTime.ToString(CultureInfo.InvariantCulture));

                    var previousFireTime = trigger.GetPreviousFireTimeUtc();
                    if (previousFireTime.HasValue)
                        InfoText("Ran At:".PadRight(15))
                            .Line(previousFireTime.Value.LocalDateTime.ToString(CultureInfo.InvariantCulture));
                });
                Newline();
            });
        }
    }
}