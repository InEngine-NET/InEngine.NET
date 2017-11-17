using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class ClearAll : AbstractBrokerCommand
    {
        [Option("processing-queue", HelpText = "Clear the in processing queue.")]
        public bool ClearProcessingQueue { get; set; }

        public override CommandResult Run()
        {
            var broker = Broker.MakeBroker(this);
            Console.WriteLine(ClearProcessingQueue ? broker.ClearProcessingQueue() : broker.ClearWaitingQueue());
            return new CommandResult(true);
        }
    }
}
