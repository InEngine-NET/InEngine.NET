using System;

namespace InEngine.Core.Exceptions;

public class CommandChainFailedException : Exception
{
    public CommandChainFailedException() : base()
    {
    }

    public CommandChainFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}