using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class ClearAll : AbstractCommand
    {
        [Option("processing-queue", HelpText = "Clear the processing queue.")]
        public bool ClearProcessingQueue { get; set; }

        [Option("secondary", DefaultValue = false, HelpText = "Clear the secondary queue.")]
        public bool UseSecondaryQueue { get; set; }

        public override CommandResult Run()
        {
            var broker = Broker.Make();
            if (UseSecondaryQueue) {
                Console.WriteLine(ClearProcessingQueue ? 
                                  broker.ClearSecondaryProcessingQueue() : 
                                  broker.ClearSecondaryWaitingQueue());    
            } else {
                Console.WriteLine(ClearProcessingQueue ? 
                                  broker.ClearPrimaryProcessingQueue() : 
                                  broker.ClearPrimaryWaitingQueue());
            }

            return new CommandResult(true);
        }
    }
}
