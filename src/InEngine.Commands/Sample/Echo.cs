using InEngine.Core;
using System;
using CommandLine;

namespace InEngine.Commands.Sample
{
    public class Echo : AbstractCommand
    {
        [Option("text")]
        public string Text { get; set; }

        public override CommandResult Run()
        {
            Console.WriteLine(Text);
            return new CommandResult(true);
        }
    }
}
