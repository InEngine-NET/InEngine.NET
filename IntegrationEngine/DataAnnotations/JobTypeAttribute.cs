using System;
using System.ComponentModel.DataAnnotations;
using IntegrationEngine.Scheduler;
using Microsoft.Practices.Unity;

namespace IntegrationEngine
{
    public class JobTypeAttribute : ValidationAttribute
    {
        public JobTypeAttribute()
        {}

        public override bool IsValid(object value)
        {
            string strValue = value as string;
            if (string.IsNullOrEmpty(strValue))
                return true;
            return ContainerSingleton.GetContainer()
                .Resolve<IEngineScheduler>()
                .IsJobTypeRegistered(strValue);
        }
    }
}

