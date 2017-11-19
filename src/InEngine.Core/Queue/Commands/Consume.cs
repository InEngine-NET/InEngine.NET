using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class Consume : AbstractCommand
    {
        [Option("all", DefaultValue = false)]
        public bool All { get; set; }

        [Option("secondary", DefaultValue = false, HelpText = "Consume from a secondary queue.")]
        public bool UseSecondaryQueue { get; set; }

        public override CommandResult Run()
        {
            var broker = Broker.Make();
            var shouldConsume = true;
            while (shouldConsume)
                shouldConsume = broker.Consume(UseSecondaryQueue) && All;
            return new CommandResult(true, "Consumed");
        }
    }
}
