using CommandLine;
using CommandLine.Text;

namespace InEngine.Core.Commands
{
    public class Options : IOptions
    {
        [VerbOption("echo", HelpText= "Echo some text to the console. Useful for end-to-end testing.")]
        public Echo Echo { get; set; }

        [VerbOption("process", HelpText = "Launch an arbitrary process.")]
        public RuntimeProcess Process { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
