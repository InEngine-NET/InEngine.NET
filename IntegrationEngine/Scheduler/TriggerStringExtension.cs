using System;

namespace IntegrationEngine.Scheduler
{
    public static class TriggerStringExtension
    {
        public static bool IsValidCronExpression(this string value)
        {
            return Quartz.CronExpression.IsValidExpression(value);
        }
    }
}

