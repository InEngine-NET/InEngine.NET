using System;
using CronExpressionDescriptor;

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
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(value);
            } 
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }

        public static string GetHumanReadableCronSchedule(this string value)
        {
            if (value == null)
                return string.Empty;
            try
            {
                return ExpressionDescriptor.GetDescription(value);
            }
            catch 
            {
                return string.Empty;
            }
        }
    }
}
