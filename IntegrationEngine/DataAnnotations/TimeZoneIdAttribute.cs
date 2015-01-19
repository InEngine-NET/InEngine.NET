using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace IntegrationEngine
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

