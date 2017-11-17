using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class Consume : AbstractBrokerCommand
    {
        [Option("all", DefaultValue = false)]
        public bool All { get; set; }

        public override CommandResult Run()
        {
            var broker = Broker.MakeBroker(this);
            var shouldConsume = true;
            while (shouldConsume)
                shouldConsume = broker.Consume() && All;
            return new CommandResult(true, "Consumed");
        }
    }
}
