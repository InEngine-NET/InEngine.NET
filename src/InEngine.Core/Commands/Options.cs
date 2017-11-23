using CommandLine;
using CommandLine.Text;

namespace InEngine.Core.Commands
{
    public class Options : IOptions
    {
        [VerbOption("null", HelpText = "Always fail. Useful for end-to-end testing.")]
        public AlwaysFail AlwaysFail { get; set; }

        [VerbOption("echo", HelpText= "Echo some text to the console. Useful for end-to-end testing.")]
        public Echo Echo { get; set; }

        [VerbOption("null", HelpText = "A null operation command. Literally does nothing.")]
        public AlwaysSucceed Null { get; set; }

        public string GetUsage(string verb)
        {
            return null;
        }
    }
}
