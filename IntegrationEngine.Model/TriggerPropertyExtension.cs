using CronExpressionDescriptor;
using System;

namespace IntegrationEngine.Model
{
    public static class TriggerPropertyExtension
    {
        public static string GetStateDescription(this int value)
        {
            return ((TriggerStateDescription)value).ToString();
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
