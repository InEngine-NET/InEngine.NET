using CommandLine;
using CommandLine.Text;

namespace InEngine.Core.Queuing.Commands
{
    public class RegisterCommands : AbstractPlugin
    {
        [VerbOption("queue:publish", HelpText = "Publish a command commandEnvelope to a queue.")]
        public Publish Publish { get; set; }

        [VerbOption("queue:consume", HelpText = "Consume one or more command messages from the queue.")]
        public Consume Consume { get; set; }

        [VerbOption("queue:length", HelpText = "Get the number of messages in the primary and secondary queues.")]
        public Length Length { get; set; }

        [VerbOption("queue:flush", HelpText = "Clear the primary or secondary queues.")]
        public Flush Flush { get; set; }

        [VerbOption("queue:republish", HelpText = "Republish failed messages to the queue.")]
        public RepublishFailed RepublishFailed { get; set; }

        [VerbOption("queue:peek", HelpText = "Peek at messages in the primary or secondary queues.")]
        public Peek Peek { get; set; }
    }
}
