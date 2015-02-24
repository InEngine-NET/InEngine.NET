using BeekmanLabs.UnitTesting;
using IntegrationEngine.Scheduler;
using NUnit.Framework;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using Moq;
using Quartz;

namespace IntegrationEngine.Tests.Scheduler
{
    public class EngineSchedulerTest : TestBase<EngineScheduler>
    {
        public CronTrigger CronTrigger { get; set; }
        public Mock<IScheduler> MockScheduler { get; set; }

        [SetUp]
        public void SetUp()
        {
            CronTrigger = new CronTrigger() {
                Id = "one",
                JobType = IntegrationJobStub.FullName,
                CronExpressionString = "* * * * * ?",
            };
            Subject.IntegrationJobTypes = new List<Type>() { IntegrationJobStub.Type };
            MockScheduler = new Mock<IScheduler>();
            Subject.Scheduler = MockScheduler.Object;
        }

        [Test]
        public void ShouldScheduleCronTriggerWithoutAnExceptionThrown()
        {
            MockScheduler.Setup(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>()));

            Subject.ScheduleJobWithTrigger(CronTrigger);

            MockScheduler.Verify(
                x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>()), 
                Times.Once);
        }

        [Test]
        public void ShouldDeleteTrigger()
        {
            var expectedGroup = "IntegrationEngine.Tests.IntegrationJobStub";
            var expectedName = "one";
            MockScheduler.Setup(x => x.UnscheduleJob(It.Is<TriggerKey>(y => y.Group == expectedGroup && y.Name == expectedName)))
                .Returns(true);

            var result = Subject.DeleteTrigger(CronTrigger);

            Assert.That(result, Is.True);
            MockScheduler.Verify(
                x => x.UnscheduleJob(It.Is<TriggerKey>(y => y.Group == expectedGroup && y.Name == expectedName)), 
                Times.Once);
        }

        [Test]
        public void ShouldReturnFalseIfATriggerThatIsNotScheduledIsDeleted()
        {
            var result = Subject.DeleteTrigger(CronTrigger);

            Assert.That(result, Is.False);
        }
    }
}
