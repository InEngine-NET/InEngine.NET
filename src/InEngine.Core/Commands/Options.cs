using CommandLine;
using CommandLine.Text;

namespace InEngine.Core.Commands
{
    public class Options : IOptions
    {
        [VerbOption("null", HelpText = "A null operation command. Literally does nothing.")]
        public Null Null { get; set; }

        [VerbOption("echo", HelpText= "Echo some text to the console. Useful for end-to-end testing.")]
        public Echo Echo { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
