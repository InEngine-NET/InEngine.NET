using CommandLine;
using CommandLine.Text;

namespace InEngine
{
    public class Options
    {
        [Option('s', "server", HelpText = "Run InEngine server.")]
        public bool ShouldRunServer { get; set; }

        [Option('c', "configuration", HelpText = "The path to the configuration file.", DefaultValue = "./appsettings.json")]
        public string ConfigurationFile { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
