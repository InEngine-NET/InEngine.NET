using System;
using System.Threading;
using CommandLine;

namespace InEngine.Core.Queuing.Commands;

public class Consume : AbstractCommand, IHasQueueSettings
{
    [Option("all", HelpText = "Consume all the messages in the primary or secondary queue.")]
    public bool ShouldConsumeAll { get; set; }

    [Option("count", HelpText = "The number of messages to consume.")]
    public int Count { get; set; } = 1;

    [Option("secondary", DefaultValue = false, HelpText = "Consume from the secondary queue.")]
    public bool UseSecondaryQueue { get; set; }

    public QueueSettings QueueSettings { get; set; }

    protected QueueAdapter QueueAdapter { get; set; }

    public bool ConsumeMessage()
    {
        try
        {
            var commandEnvelope = QueueAdapter.Consume();
            return commandEnvelope != null;
        }
        catch (Exception exception)
        {
            Error(exception.Message);
            return false;
        }
    }

    public override void Run()
    {
        QueueAdapter = QueueAdapter.Make(UseSecondaryQueue, QueueSettings, MailSettings);

        if (ShouldConsumeAll)
        {
            while (ShouldConsumeAll)
                if (!ConsumeMessage())
                    Thread.Sleep(1000);
        }
        else
        {
            for (var i = 0; i < Count; i++)
                if (!ConsumeMessage())
                    break;   
        }

        Line("Finished consuming messages.");
    }

    public override void Failed(Exception exception)
    {
        Error(exception.Message);
    }
}