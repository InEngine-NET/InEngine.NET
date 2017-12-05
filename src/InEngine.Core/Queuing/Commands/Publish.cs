using System;
using System.Linq;
using CommandLine;
using InEngine.Core.Exceptions;

namespace InEngine.Core.Queuing.Commands
{
    public class Publish : AbstractCommand
    {
        [Option("command-plugin", Required = true, HelpText = "The name of a command plugin file, e.g. InEngine.Core.dll")]
        public string CommandPlugin { get; set; }

        [Option("command-verb", HelpText =  "A plugin command verb, e.g. echo.")]
        public string CommandVerb { get; set; }

        [Option("command-class", HelpText = "A command class name, e.g. InEngine.Core.Commands.AlwaysSucceed. Takes precedence over --command-verb if both are specified.")]
        public string CommandClass { get; set; }

        [OptionArray("args", HelpText = "An optional list of arguments to publish with the command.")]
        public string[] Arguments { get; set; }

        [Option("secondary", DefaultValue = false, HelpText = "Publish the command to the secondary queue.")]
        public bool UseSecondaryQueue { get; set; }

        public AbstractCommand Command { get; set; }
        public QueueAdapter Queue { get; set; }

        public override void Run()
        {
            var command = Command;

            if (command == null && !string.IsNullOrWhiteSpace(CommandPlugin)) {
                var plugin = Plugin.LoadFrom(CommandPlugin);
                if (!string.IsNullOrWhiteSpace(CommandClass))
                    command = plugin.CreateCommandFromClass(CommandClass);
                else if (!string.IsNullOrWhiteSpace(CommandVerb)) {
                    command = plugin.CreateCommandFromVerb(CommandVerb);
                }
            }

            if (command == null)
                throw new CommandFailedException("Did not publish command. Could not load command from plugin.");

            if (Arguments != null)
                Parser.Default.ParseArguments(Arguments.ToList().Select(x => $"--{x}").ToArray(), command);

            if (Queue == null)
                Queue = QueueAdapter.Make(UseSecondaryQueue);

            Queue.Publish(command);
        }

        public override void Failed(Exception exception)
        {
            Write.Error(exception.Message);
        }
    }
}
