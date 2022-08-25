using System;

namespace InEngine.Core.Exceptions
{
    public class CommandNotFoundException : Exception
    {
        public CommandNotFoundException(string command)
            : base($"Command not found: {command}")
        {
        }
    }
}