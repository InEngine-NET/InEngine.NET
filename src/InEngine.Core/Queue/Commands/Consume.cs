using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class Consume : AbstractCommand
    {
        [Option("all", HelpText = "Consume all the messages in the primary or secondary queue.")]
        public bool ShouldConsumeAll { get; set; }

        [Option("secondary", DefaultValue = false, HelpText = "Consume from the secondary queue.")]
        public bool UseSecondaryQueue { get; set; }

        public override CommandResult Run()
        {
            UseSecondaryQueue = UseSecondaryQueue || GetJobContextData<bool>("useSecondaryQueue");
            var broker = Broker.Make();
            var shouldConsume = true;
            while (shouldConsume)
                shouldConsume = broker.Consume(UseSecondaryQueue) && ShouldConsumeAll;
            return new CommandResult(true, "Consumed");
        }
    }
}
