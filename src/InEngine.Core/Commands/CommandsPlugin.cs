using CommandLine;

namespace InEngine.Core.Commands
{
    public class CommandPlugin : AbstractPlugin
    {
        [VerbOption("echo", HelpText= "Echo some text to the console. Useful for end-to-end testing.")]
        public Echo Echo { get; set; }

        [VerbOption("proc", HelpText = "Launch an arbitrary process.")]
        public SystemProcess SystemProcess { get; set; }
    }
}
