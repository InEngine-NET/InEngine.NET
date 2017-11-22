using CommandLine;
using CommandLine.Text;
using InEngine.Commands.Sample;
using InEngine.Core;

namespace InEngine.Commands
{
    public class MoreOptions : IOptions
    {
        [VerbOption("sample:show-progress", HelpText = "A sample command to demonstrate the progress bar.")]
        public ShowProgress ShowProgress { get; set; }

        [VerbOption("sample:say-hello", HelpText = "A sample command to say \"hello\".")]
        public SayHello SayHello { get; set; }

        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
