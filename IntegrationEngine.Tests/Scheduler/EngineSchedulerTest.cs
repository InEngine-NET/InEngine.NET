using IntegrationEngine.Scheduler;
using NUnit.Framework;
using Quartz.Impl;
using System;
using System.Collections.Generic;

namespace IntegrationEngine.Tests.Scheduler
{
    public class EngineSchedulerTest
    {
        [Test]
        public void ShouldDeleteTrigger()
        {
            var jobType = typeof(IntegrationJobFixture);
            var subject = new EngineScheduler() {
                IntegrationJobTypes = new List<Type>() { jobType }
            };
            var trigger = new CronTrigger() {
                JobType = jobType.FullName
            };

            subject.DeleteTrigger(trigger);


        }
    }
}
