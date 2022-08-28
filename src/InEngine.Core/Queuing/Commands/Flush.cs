using CommandLine;
using InEngine.Core.Exceptions;

namespace InEngine.Core.Queuing.Commands;

public class Flush : AbstractCommand, IHasQueueSettings
{
    [Option("pending", HelpText = "Clear the pending queue.")]
    public bool PendingQueue { get; set; }

    [Option("failed", HelpText = "Clear the failed queue.")]
    public bool FailedQueue { get; set; }

    [Option("in-progress", HelpText = "Clear the in-progress queue.")]
    public bool InProgressQueue { get; set; }

    [Option("secondary", HelpText = "Clear secondary queues. Primary queues are cleared by default.")]
    public bool UseSecondaryQueue { get; set; }

    public QueueSettings QueueSettings { get; set; }

    public override void Run()
    {
        if (PendingQueue == false && FailedQueue == false && InProgressQueue == false)
            throw new CommandFailedException("Must specify at least one queue to clear. Use -h to see available options.");
        var queue = QueueAdapter.Make(UseSecondaryQueue, QueueSettings, MailSettings);
        if (PendingQueue)
            Info($"Pending: {queue.ClearPendingQueue().ToString()}");
        if (InProgressQueue)
            Info($"In-progress: {queue.ClearInProgressQueue().ToString()}");
        if (FailedQueue)
            Info($"Failed: {queue.ClearFailedQueue().ToString()}");
    }
}