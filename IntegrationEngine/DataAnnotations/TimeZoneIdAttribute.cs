using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace IntegrationEngine.DataAnnotations
{
    public class TimeZoneIdAttribute : ValidationAttribute
    {
        public TimeZoneIdAttribute()
        {}

        public override bool IsValid(object value)
        {
            string strValue = value as string;
            if (string.IsNullOrEmpty(strValue))
                return true;
            return TimeZoneInfo.GetSystemTimeZones().Where(x => x.Id == strValue).Any();
        }
    }
}

