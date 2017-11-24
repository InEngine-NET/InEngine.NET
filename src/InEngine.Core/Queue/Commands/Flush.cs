using CommandLine;
using InEngine.Core.Exceptions;

namespace InEngine.Core.Queue.Commands
{
    public class Flush : AbstractCommand
    {
        [Option("pending", HelpText = "Clear the pending queue.")]
        public bool ClearPendingQueue { get; set; }

        [Option("failed", HelpText = "Clear the failed queue.")]
        public bool ClearFailedQueue { get; set; }

        [Option("in-progress", HelpText = "Clear the in-progress queue.")]
        public bool ClearInProgressQueue { get; set; }

        [Option("secondary", HelpText = "Clear secondary queues. Primary queues are cleared by default.")]
        public bool UseSecondaryQueue { get; set; }

        public override void Run()
        {
            if (ClearPendingQueue == false && ClearFailedQueue == false && ClearInProgressQueue == false)
                throw new CommandFailedException("Must specify at least one queue to clear. Use -h to see available options.");
            var broker = Broker.Make(UseSecondaryQueue);
            if (ClearPendingQueue)
                Info($"Pending: {broker.ClearPendingQueue().ToString()}");
            if (ClearInProgressQueue)
                Info($"In-progress: {broker.ClearInProgressQueue().ToString()}");
            if (ClearFailedQueue)
                Info($"Failed: {broker.ClearFailedQueue().ToString()}");
        }
    }
}
