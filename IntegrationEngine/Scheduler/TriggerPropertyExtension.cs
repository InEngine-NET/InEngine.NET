using System;

namespace IntegrationEngine.Scheduler
{
    public static class TriggerPropertyExtension
    {
        public static bool IsValidCronExpression(this string value)
        {
            return Quartz.CronExpression.IsValidExpression(value);
        }

        public static bool IsValidTimeZone(this string value)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetStateDescription(this int value)
        {
            return ((Quartz.TriggerState)value).ToString();
        }
    }
}
