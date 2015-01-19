﻿using System;
using System.ComponentModel.DataAnnotations;

namespace IntegrationEngine
{
    public class CronExpressionStringAttribute : ValidationAttribute
    {
        public CronExpressionStringAttribute()
        {}

        public override bool IsValid(object value)
        {
            string strValue = value as string;
            if (string.IsNullOrEmpty(strValue))
                return true;
            return Quartz.CronExpression.IsValidExpression(strValue);
        }
    }
}

