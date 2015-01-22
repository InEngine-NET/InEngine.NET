using IntegrationEngine.Scheduler;
using Moq;
using NUnit.Framework;
using Quartz;
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
                Id = "one",
                JobType = jobType.FullName
            };
            var scheduler = new Mock<IScheduler>();
            scheduler.Setup(x => x.UnscheduleJob(It.Is<TriggerKey>(y => y.Name == trigger.Id && 
                                                                        y.Group == trigger.JobType))).Returns(true);
            subject.Scheduler = scheduler.Object;

            var result = subject.DeleteTrigger(trigger);

            Assert.That(result, Is.True);
        }
    }
}
