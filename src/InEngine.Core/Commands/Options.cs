using CommandLine;
using CommandLine.Text;

namespace InEngine.Core.Commands
{
    public class Options : IOptions
    {
        [VerbOption("fail", HelpText = "Always fail. Useful for end-to-end testing.")]
        public AlwaysFail AlwaysFail { get; set; }

        [VerbOption("echo", HelpText= "Echo some text to the console. Useful for end-to-end testing.")]
        public Echo Echo { get; set; }

        [VerbOption("succeed", HelpText = "A null operation command. Literally does nothing.")]
        public AlwaysSucceed Null { get; set; }

        public string GetUsage(string verb)
        {
            return null;
        }
    }
}
