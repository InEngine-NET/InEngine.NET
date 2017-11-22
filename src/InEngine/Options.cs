using CommandLine;
using CommandLine.Text;

namespace InEngine
{
    public class Options
    {
        [Option('p', "plugin", HelpText = "Plug-In to activate.")]
        public string PluginName { get; set; }

        [Option('s', "scheduler", HelpText = "Run the scheduler.", MutuallyExclusiveSet = "PluginName")]
        public bool ShouldRunScheduler { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
