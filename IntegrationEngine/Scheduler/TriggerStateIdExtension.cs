using Quartz;
using System;

namespace IntegrationEngine.Scheduler
{
    public static class TriggerStateIdExtension
    {
        public static string GetStateDescription(this int value)
        {
            return ((TriggerState)value).ToString();
        }
    }
}

