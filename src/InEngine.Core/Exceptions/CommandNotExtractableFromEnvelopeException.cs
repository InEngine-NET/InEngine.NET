using System;

namespace InEngine.Core.Exceptions;

public class CommandNotExtractableFromEnvelopeException : Exception
{
    public CommandNotExtractableFromEnvelopeException(string command, Exception exception)
        : base(
            $"The plugin is (probably) not registered - check the message in the failure queue for details. Command name: {command}",
            exception
        )
    {
    }
}