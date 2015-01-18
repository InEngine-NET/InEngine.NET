using System;

namespace IntegrationEngine.Scheduler
{
    public static class TriggerCronExpressionExtension
    {
        public static bool IsValidCronExpression(this string value)
        {
            return Quartz.CronExpression.IsValidExpression(value);
        }
    }
}

