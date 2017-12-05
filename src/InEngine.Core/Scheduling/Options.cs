using CommandLine;
using CommandLine.Text;
using InEngine.Core.Queuing.Commands;

namespace InEngine.Core.Scheduling
{
    public class Options : IOptions
    {
        [VerbOption("schedule:list", HelpText = "List all scheduled jobs.")]
        public ListScheduledCommands ListScheduledCommands { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
