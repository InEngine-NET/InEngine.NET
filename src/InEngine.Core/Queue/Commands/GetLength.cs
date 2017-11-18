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
            Console.WriteLine(CheckProcessingQueue ? 
                              broker.GetProcessingQueueLength() :
                              broker.GetWaitingQueueLength());
            return new CommandResult(true);
        }
    }
}
