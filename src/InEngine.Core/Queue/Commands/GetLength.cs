using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class GetLength : AbstractCommand
    {
        [Option("processing-queue", HelpText = "Display the number of running commands.")]
        public bool CheckProcessingQueue { get; set; }

        public override CommandResult Run()
        {
            var broker = Broker.Make();
            Console.WriteLine("[Primary Queue] Pending Messages: " + broker.GetPrimaryWaitingQueueLength());
            Console.WriteLine("[Primary Queue] Processing Queue: " + broker.GetPrimaryProcessingQueueLength());

            Console.WriteLine("[Secondary Queue] Pending Messages: " + broker.GetSecondaryWaitingQueueLength());
            Console.WriteLine("[Secondary Queue] Processing Queue: " + broker.GetSecondaryProcessingQueueLength());

            return new CommandResult(true);
        }
    }
}
