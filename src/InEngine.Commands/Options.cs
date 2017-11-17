using CommandLine;
using CommandLine.Text;
using InEngine.Commands.Sample;
using InEngine.Core;

namespace InEngine.Commands
{
    public class Options : IOptions
    {
        [VerbOption("sample:minimal")]
        public Minimal Minimal { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
