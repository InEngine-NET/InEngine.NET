using System;

namespace InEngine.Core.Exceptions
{
    public class AmbiguousCommandException : Exception
    {
        public AmbiguousCommandException(string commandName)
            : base($"Command name found in multiple plugins: {commandName}")
        {
        }
    }
}