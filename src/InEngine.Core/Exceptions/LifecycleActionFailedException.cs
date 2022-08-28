using System;

namespace InEngine.Core.Exceptions
{
    public class LifecycleActionFailedException : Exception
    {
        public LifecycleActionFailedException(string message, Exception exception) : base(message, exception)
        {
        }
    }
}