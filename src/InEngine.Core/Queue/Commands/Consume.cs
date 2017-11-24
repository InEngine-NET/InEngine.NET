using System;
using CommandLine;
using Quartz;

namespace InEngine.Core.Queue.Commands
{
    public class Consume : AbstractCommand
    {
        [Option("all", HelpText = "Consume all the messages in the primary or secondary queue.")]
        public bool ShouldConsumeAll { get; set; }

        [Option("secondary", DefaultValue = false, HelpText = "Consume from the secondary queue.")]
        public bool UseSecondaryQueue { get; set; }

        public override void Run()
        {
            UseSecondaryQueue = UseSecondaryQueue || GetJobContextData<bool>("useSecondaryQueue");
            var broker = Broker.Make(UseSecondaryQueue);
            var shouldConsume = true;
            while (shouldConsume)
                shouldConsume = broker.Consume() && ShouldConsumeAll;
        }

        public override void Failed(Exception exception)
        {
            Write.Error(exception.Message);
        }
    }
}
