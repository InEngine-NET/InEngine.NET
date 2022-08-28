using System;
using System.Threading;
using CommandLine;
using InEngine.Core.Queuing.Message;

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

    public override void Run()
    {
        var queue = QueueAdapter.Make(UseSecondaryQueue, QueueSettings, MailSettings);
        ICommandEnvelope commandEnvelope;
        while (ShouldConsumeAll)
            try
            {
                commandEnvelope = queue.Consume();
                if (commandEnvelope == null) 
                    Thread.Sleep(5000);
            }
            catch (Exception exception)
            {
                Error(exception.Message);
            }

        if (ShouldConsumeAll)
            return;

        for (var i = 0; i < Count; i++)
            try
            {
                commandEnvelope = queue.Consume();
                if (commandEnvelope == null)
                    return;
            }
            catch (Exception exception)
            {
                Error(exception.Message);
            }
    }

    public override void Failed(Exception exception)
    {
        Error(exception.Message);
    }
}