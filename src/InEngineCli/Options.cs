using CommandLine;
using CommandLine.Text;
using InEngine.Commands.Sample;
using InEngine.Core.Queue.Commands;

namespace InEngineCli
{
    public class Options
    {
        [Option('p', "plugin", Required = true, HelpText = "Plug-In to activate.")]
        public string PlugInName { get; set; }


        [VerbOption("queue:publish")]
        public Publish QueuePublish { get; set; }

        [VerbOption("queue:consume")]
        public Consume QueueConsume { get; set; }

        [VerbOption("queue:length")]
        public GetLength QueueGetLength { get; set; }

        [VerbOption("queue:clear")]
        public ClearAll QueueClearAll { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
