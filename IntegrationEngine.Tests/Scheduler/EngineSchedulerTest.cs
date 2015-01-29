using BeekmanLabs.UnitTesting;
using IntegrationEngine.Scheduler;
using NUnit.Framework;
using Quartz.Impl;
using System;
using System.Collections.Generic;

namespace IntegrationEngine.Tests.Scheduler
{
    public class EngineSchedulerTest : TestBase<EngineScheduler>
    {
        public CronTrigger CronTrigger { get; set; }
        [SetUp]
        public void SetUp()
        {
            CronTrigger = new CronTrigger() {
                Id = "one",
                JobType = IntegrationJobFixture.FullName,
                CronExpressionString = "* * * * * ?",
            };
            Subject.IntegrationJobTypes = new List<Type>() { IntegrationJobFixture.Type };
            Subject.Scheduler = StdSchedulerFactory.GetDefaultScheduler();
        }

        [Test]
        public void ShouldScheduleCronTriggerWithoutAnExceptionThrown()
        {
            Subject.ScheduleJobWithTrigger(CronTrigger);
        }

        [Test]
        public void ShouldDeleteTrigger()
        {
            Subject.ScheduleJobWithTrigger(CronTrigger);

            var result = Subject.DeleteTrigger(CronTrigger);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldReturnFalseIfATriggerThatIsNotScheduledIsDeleted()
        {
            var result = Subject.DeleteTrigger(CronTrigger);

            Assert.That(result, Is.False);
        }
    }
}
