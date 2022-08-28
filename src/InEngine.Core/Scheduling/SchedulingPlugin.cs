using CommandLine;

namespace InEngine.Core.Scheduling;

using Commands;

public class SchedulingPlugin : AbstractPlugin
{
    [VerbOption("schedule:list", HelpText = "List all scheduled jobs.")]
    public ListScheduledCommands ListScheduledCommands { get; set; }
}