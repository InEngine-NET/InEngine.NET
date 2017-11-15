using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class GetLength : AbstractBrokerCommand
    {
        [Option("processing-queue", HelpText = "Display the number of running commands.")]
        public bool CheckProcessingQueue { get; set; }

        public override CommandResult Run()
        {
            var broker = Broker.MakeBroker(this);
            Console.WriteLine(CheckProcessingQueue ? 
                              broker.GetProcessingQueueLength() :
                              broker.GetWaitingQueueLength());
            return new CommandResult(true);
        }
    }
}
