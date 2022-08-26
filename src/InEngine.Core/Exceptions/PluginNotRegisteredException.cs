using System;

namespace InEngine.Core.Exceptions;

public class PluginNotRegisteredException : Exception
{
    public PluginNotRegisteredException(string message = "") : base(message)
    {
    }

    public PluginNotRegisteredException(string message, Exception innerException) : base(message, innerException)
    {
    }
}