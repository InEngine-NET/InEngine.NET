using System;
using InEngine.Core.Exceptions;

namespace InEngine.Core.Queuing.Message
{
    public class CommandEnvelope : ICommandEnvelope
    {
        public int Id { get; set; }
        public string PluginName { get; set; }
        public string CommandClassName { get; set; }
        public string SerializedCommand { get; set; }
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
        public bool IsCompressed { get; set; }

        public AbstractCommand GetCommandInstance()
        {
            var commandType = PluginAssembly.LoadFrom(PluginName).GetCommandType(CommandClassName);
            if (commandType == null)
                throw new CommandFailedException($"Could not locate command {CommandClassName}. Is the {PluginName} plugin registered in the settings file?");
            return SerializedCommand.DeserializeFromJson<AbstractCommand>(IsCompressed);
        }
    }
}
