using CommandLine;
using CommandLine.Text;
using InEngine.Core.Queue.Commands;

namespace InEngine.Core.Queue
{
    public class Options : IOptions
    {
        [VerbOption("queue:publish", HelpText = "Publish a command message to a queue.")]
        public Publish QueuePublish { get; set; }

        [VerbOption("queue:consume", HelpText = "Consume one or more command messages from the queue.")]
        public Consume QueueConsume { get; set; }

        [VerbOption("queue:length", HelpText = "Get the number of messages in the primary and secondary queues.")]
        public GetLength QueueGetLength { get; set; }

        [VerbOption("queue:clear", HelpText = "Clear the primary and secondary queues.")]
        public ClearAll QueueClearAll { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
