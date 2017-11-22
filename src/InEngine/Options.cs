using CommandLine;
using CommandLine.Text;

namespace InEngine
{
    public class Options
    {
        [Option('p', "plugin", HelpText = "Plug-In to activate.")]
        public string PluginName { get; set; }

        [Option('s', "scheduler", HelpText = "Run the scheduler.")]
        public bool ShouldRunScheduler { get; set; }

        [Option('c', "configuration", HelpText = "The path to the configuration file.", DefaultValue = "./appsettings.json")]
        public string ConfigurationFile { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
