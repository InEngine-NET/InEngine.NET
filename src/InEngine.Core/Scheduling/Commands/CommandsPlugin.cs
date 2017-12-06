using CommandLine;

namespace InEngine.Core.Scheduling
{
    public class CommandsPlugin : AbstractPlugin
    {
        [VerbOption("schedule:list", HelpText = "List all scheduled jobs.")]
        public ListScheduledCommands ListScheduledCommands { get; set; }
    }
}
