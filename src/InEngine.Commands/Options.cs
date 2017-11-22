using CommandLine;
using InEngine.Commands.Sample;
using InEngine.Core;

namespace InEngine.Commands
{
    public class Options : IOptions
    {
        [VerbOption("sample:minimal")]
        public Minimal Minimal { get; set; }

        public string GetUsage(string verb)
        {
            return "\tsample:minimal \t\tA minimal implementation of a command.";
        }
    }
}
