using IntegrationEngine.Scheduler;
using Moq;
using NUnit.Framework;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using UnitTesting;

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
            Subject.ScheduleJobWithCronTrigger(CronTrigger);
        }

        [Test]
        public void ShouldDeleteTrigger()
        {
            //var scheduler = new Mock<IScheduler>();
            //scheduler.Setup(x => x.UnscheduleJob(It.Is<TriggerKey>(y => y.Name == trigger.Id && 
            //                                                            y.Group == trigger.JobType))).Returns(true);
            //Subject.Scheduler = scheduler.Object;
            Subject.ScheduleJobWithCronTrigger(CronTrigger);

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
