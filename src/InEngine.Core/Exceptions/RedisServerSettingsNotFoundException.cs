using System;

namespace InEngine.Core.Exceptions
{
    public class RedisServerSettingsNotFoundException : Exception
    {
        public RedisServerSettingsNotFoundException(string instanceName) 
            : base(instanceName)
        {
        }
    }
}
