using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class GetLength : AbstractCommand
    {
        public override void Run()
        {
            var broker = Broker.Make();
            Console.WriteLine("[Primary Queue] Pending Messages: " + broker.GetPrimaryWaitingQueueLength());
            Console.WriteLine("[Primary Queue] Processing Queue: " + broker.GetPrimaryProcessingQueueLength());

            Console.WriteLine("[Secondary Queue] Pending Messages: " + broker.GetSecondaryWaitingQueueLength());
            Console.WriteLine("[Secondary Queue] Processing Queue: " + broker.GetSecondaryProcessingQueueLength());
        }
    }
}
