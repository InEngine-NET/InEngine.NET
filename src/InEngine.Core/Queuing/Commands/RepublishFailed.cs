using System;
using System.Linq;
using CommandLine;

namespace InEngine.Core.Queuing.Commands
{
    public class RepublishFailed : AbstractCommand
    {
        [Option("limit", DefaultValue = 100, HelpText = "The maximum number of messages to republish.")]
        public int Limit { get; set; }

        [Option("secondary", DefaultValue = false, HelpText = "Republish failed secondary queue messages.")]
        public bool UseSecondaryQueue { get; set; }

        public override void Run()
        {
            var broker = Queue.Make(UseSecondaryQueue);
            Enumerable.Range(0, Limit)
                      .ToList()
                      .ForEach(x => broker.RepublishFailedMessages());
        }
    }
}
