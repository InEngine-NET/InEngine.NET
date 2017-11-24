using CommandLine;
using InEngine.Core.Exceptions;

namespace InEngine.Core.Queue.Commands
{
    public class Flush : AbstractCommand
    {
        [Option("pending", HelpText = "Clear the pending queue.")]
        public bool PendingQueue { get; set; }

        [Option("failed", HelpText = "Clear the failed queue.")]
        public bool FailedQueue { get; set; }

        [Option("in-progress", HelpText = "Clear the in-progress queue.")]
        public bool InProgressQueue { get; set; }

        [Option("secondary", HelpText = "Clear secondary queues. Primary queues are cleared by default.")]
        public bool UseSecondaryQueue { get; set; }

        public override void Run()
        {
            if (PendingQueue == false && FailedQueue == false && InProgressQueue == false)
                throw new CommandFailedException("Must specify at least one queue to clear. Use -h to see available options.");
            var broker = Broker.Make(UseSecondaryQueue);
            if (PendingQueue)
                Info($"Pending: {broker.ClearPendingQueue().ToString()}");
            if (InProgressQueue)
                Info($"In-progress: {broker.ClearInProgressQueue().ToString()}");
            if (FailedQueue)
                Info($"Failed: {broker.ClearFailedQueue().ToString()}");
        }
    }
}
