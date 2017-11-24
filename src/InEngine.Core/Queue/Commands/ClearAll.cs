using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class ClearAll : AbstractCommand
    {
        [Option("failed-queue", HelpText = "Clear the failed queue.")]
        public bool ClearFailedQueue { get; set; }

        [Option("processing-queue", HelpText = "Clear the processing queue.")]
        public bool ClearProcessingQueue { get; set; }

        [Option("secondary", HelpText = "Clear the secondary queue.")]
        public bool UseSecondaryQueue { get; set; }

        public override void Run()
        {
            var broker = Broker.Make();
            if (UseSecondaryQueue) {
                if (ClearProcessingQueue)
                    Info(broker.ClearSecondaryProcessingQueue().ToString());
                else if (ClearFailedQueue)
                    Info(broker.ClearSecondaryFailedQueue().ToString());
                else
                    Info(broker.ClearSecondaryWaitingQueue().ToString());
            } else {
                if (ClearProcessingQueue)
                    Info(broker.ClearPrimaryProcessingQueue().ToString());
                else if (ClearFailedQueue)
                    Info(broker.ClearPrimaryFailedQueue().ToString());
                else
                    Info(broker.ClearPrimaryWaitingQueue().ToString());
            }
        }
    }
}
