using InEngine.Core;
using System;

namespace InEngine.Commands.Sample
{
    public class SayHello : AbstractCommand
    {
        public override CommandResult Run()
        {
            Console.WriteLine("hello");
            return new CommandResult(true);
        }
    }
}
