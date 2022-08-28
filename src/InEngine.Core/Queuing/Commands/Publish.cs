using System.Threading.Tasks;
using System;
using System.Linq;
using CommandLine;
using InEngine.Core.Exceptions;

namespace InEngine.Core.Queuing.Commands;

public class Publish : AbstractCommand, IHasQueueSettings
{
    [Option("plugin", Required = true, HelpText = "The name of a command plugin file, e.g. InEngine.Core")]
    public string PluginName { get; set; }

    [Option("command", HelpText =  "A command name, e.g. echo.")]
    public string CommandVerb { get; set; }

    [Option("class", HelpText = "A command class, e.g. InEngine.Core.Commands.AlwaysSucceed. Takes precedence over --command if both are specified.")]
    public string CommandClass { get; set; }

    [OptionArray("args", HelpText = "An optional list of arguments to publish with the command.")]
    public string[] Arguments { get; set; }

    [Option("secondary", DefaultValue = false, HelpText = "Publish the command to the secondary queue.")]
    public bool UseSecondaryQueue { get; set; }

    public AbstractCommand Command { get; set; }

    public QueueSettings QueueSettings { get; set; }

    public override async Task Run()
    {
        var command = Command;

        if (command == null && !string.IsNullOrWhiteSpace(PluginName)) {
            var plugin = PluginAssembly.LoadFrom(PluginName);
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

        QueueAdapter.Make(UseSecondaryQueue, QueueSettings, MailSettings).Publish(command);
    }

    public override void Failed(Exception exception)
    {
        Write.Error(exception.Message);
    }
}