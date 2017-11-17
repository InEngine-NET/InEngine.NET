using CommandLine;
using CommandLine.Text;
using InEngine.Core.Queue.Commands;

namespace InEngine.Core.Queue
{
    public class Options : IOptions
    {
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
