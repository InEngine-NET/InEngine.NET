using CommandLine;
using CommandLine.Text;
using InEngine.Commands.Sample;
using InEngine.Core.Queue.Commands;

namespace InEngineCli
{
    public class Options
    {
        [Option('p', "plugin", Required = true, HelpText = "Plug-In to activate.", DefaultValue = "InEngine.Core")]
        public string PlugInName { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
