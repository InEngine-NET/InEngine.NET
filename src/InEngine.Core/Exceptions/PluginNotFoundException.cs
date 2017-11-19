using System;

namespace InEngine.Core.Exceptions
{
    public class PluginNotFoundException : Exception
    {
        public PluginNotFoundException(string message = "") : base(message)
        {
        }

        public PluginNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
