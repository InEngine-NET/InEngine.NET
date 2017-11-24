using System;
using System.Linq;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class RepublishFailed : AbstractCommand
    {
        [Option("count", DefaultValue = 3, HelpText = "The maximum number of messages to republish.")]
        public int Count { get; set; }

        [Option("secondary", DefaultValue = false, HelpText = "Republish failed secondary queue messages.")]
        public bool UseSecondaryQueue { get; set; }

        public override void Run()
        {
            var broker = Broker.Make(UseSecondaryQueue);
            Enumerable.Range(0, Count)
                      .ToList()
                      .ForEach(x => broker.RepublishFailedMessages());
        }
    }
}
