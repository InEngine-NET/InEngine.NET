using System;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using InEngine.Core.Queuing.Commands;
using InEngine.Core.Scheduling;

namespace InEngine.Core.Queuing;

public class QueuingPlugin : AbstractPlugin
{
    [VerbOption("queue:status", HelpText = "Get the status of queues and consumers.")]
    public Status Status { get; set; }

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