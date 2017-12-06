using CommandLine;
using InEngine.Core;

namespace InEngine.Commands
{
    public class CommandsPlugin : AbstractPlugin
    {
        [VerbOption("fail", HelpText = "Always fail. Useful for end-to-end testing.")]
        public AlwaysFail AlwaysFail { get; set; }

        [VerbOption("succeed", HelpText = "A null operation command. Literally does nothing.")]
        public AlwaysSucceed Null { get; set; }
    }
}
