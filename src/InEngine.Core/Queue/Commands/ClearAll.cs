using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class ClearAll : AbstractCommand
    {
        [Option("processing-queue", HelpText = "Clear the processing queue.")]
        public bool ClearProcessingQueue { get; set; }

        [Option("secondary", HelpText = "Clear the secondary queue.")]
        public bool UseSecondaryQueue { get; set; }

        public override void Run()
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
        }
    }
}
