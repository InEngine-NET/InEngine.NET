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

        public AbstractCommand GetCommandInstanceAndIncrementRetry(Action actionOnFail = null)
        {
            try
            {
                PluginAssembly.LoadFrom(PluginName).GetCommandType(CommandClassName);
                var command = SerializedCommand.DeserializeFromJson<AbstractCommand>(IsCompressed);
                command.CommandLifeCycle.IncrementRetry();
                SerializedCommand = command.SerializeToJson(IsCompressed);
                return command;
            }
            catch (Exception exception)
            {
                actionOnFail.Invoke();
                throw new CommandNotExtractableFromEnvelopeException(CommandClassName, exception);
            }
        }
    }
}
