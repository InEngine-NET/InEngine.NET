using CommandLine;
using InEngine.Core;

namespace InEngine.Commands.Sample
{
    public class RegisterCommands : AbstractPlugin
    {
        [VerbOption("sample:show-progress", HelpText = "A sample command to demonstrate the progress bar.")]
        public ShowProgress ShowProgress { get; set; }

        [VerbOption("sample:say-hello", HelpText = "A sample command to say \"hello\".")]
        public SayHello SayHello { get; set; }

        [VerbOption("sample:minimal")]
        public Minimal Minimal { get; set; }
    }
}
