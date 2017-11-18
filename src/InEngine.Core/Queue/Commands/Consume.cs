using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class Consume : AbstractCommand
    {
        [Option("all", DefaultValue = false)]
        public bool All { get; set; }

        public override CommandResult Run()
        {
            var broker = Broker.Make();
            var shouldConsume = true;
            while (shouldConsume)
                shouldConsume = broker.Consume() && All;
            return new CommandResult(true, "Consumed");
        }
    }
}
