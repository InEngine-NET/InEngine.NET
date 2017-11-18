using System.Reflection;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class Publish : AbstractCommand
    {
        [Option("command-assembly", DefaultValue = "InEngine.Core.dll")]
        public string CommandAssembly { get; set; }

        [Option("command-class", DefaultValue = "InEngine.Core.Queue.Commands.Null")]
        public string CommandClass { get; set; }

        public override CommandResult Run()
        {
            var command = Assembly.LoadFrom(CommandAssembly).CreateInstance(CommandClass) as ICommand;
            if (command == null)
                return new CommandResult(false, "Did not publish message. Could not load command from plugin.");
            Broker.Make().Publish(command);
            return new CommandResult(true, "Published");
        }
    }
}
