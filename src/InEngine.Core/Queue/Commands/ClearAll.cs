using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class ClearAll : AbstractCommand
    {
        [Option("failed", HelpText = "Clear the failed queue.")]
        public bool ClearFailedQueue { get; set; }

        [Option("in-progress", HelpText = "Clear the processing queue.")]
        public bool ClearProcessingQueue { get; set; }

        [Option("secondary", HelpText = "Clear the secondary queue.")]
        public bool UseSecondaryQueue { get; set; }

        public override void Run()
        {
            var broker = Broker.Make(UseSecondaryQueue);
            if (ClearProcessingQueue)
                Info(broker.ClearInProgressQueue().ToString());
            else if (ClearFailedQueue)
                Info(broker.ClearFailedQueue().ToString());
            else
                Info(broker.ClearPendingQueue().ToString());
        }
    }
}
