using System;

namespace IntegrationEngine.Scheduler
{
    public static class TriggerPropertyExtension
    {
        public static string GetStateDescription(this int value)
        {
            return ((Quartz.TriggerState)value).ToString();
        }

        public static TimeZoneInfo GetTimeZoneInfo(this string value)
        {
            if (value == null)
                return TimeZoneInfo.Utc;
            return TimeZoneInfo.FindSystemTimeZoneById(value);
        }
    }
}
