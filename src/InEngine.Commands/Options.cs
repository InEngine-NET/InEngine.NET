using CommandLine;
using CommandLine.Text;
using InEngine.Core;

namespace InEngine.Commands
{
    public class Options : IOptions
    {
        [VerbOption("fail", HelpText = "Always fail. Useful for end-to-end testing.")]
        public AlwaysFail AlwaysFail { get; set; }

        [VerbOption("succeed", HelpText = "A null operation command. Literally does nothing.")]
        public AlwaysSucceed Null { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
