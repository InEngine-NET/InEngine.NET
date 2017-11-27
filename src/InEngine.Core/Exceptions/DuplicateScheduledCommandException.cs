using System;

namespace InEngine.Core.Exceptions
{
    public class DuplicateScheduledCommandException : Exception
    {
        public DuplicateScheduledCommandException(string commandName, string schedulerId, string schedulerGroup) 
            : base($"A command was scheduled multiple times with the same ID ({schedulerId}) in the same group ({schedulerGroup}). Ensure IDs are unique in a group.")
        {}
    }
}
