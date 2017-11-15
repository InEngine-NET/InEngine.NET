using System;

namespace InEngine.Core.Exceptions
{
    public class CommandFailedException : Exception
    {
        public CommandFailedException(string message) : base(message)
        {
        }

        public CommandFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
