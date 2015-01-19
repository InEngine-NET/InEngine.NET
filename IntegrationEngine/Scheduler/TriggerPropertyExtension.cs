using System;

namespace IntegrationEngine.Scheduler
{
    public static class TriggerPropertyExtension
    {
        public static string GetStateDescription(this int value)
        {
            return ((Quartz.TriggerState)value).ToString();
        }
    }
}
