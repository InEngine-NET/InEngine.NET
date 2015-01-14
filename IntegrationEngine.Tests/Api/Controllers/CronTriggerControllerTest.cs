using IntegrationEngine;
using IntegrationEngine.Api.Controllers;
using IntegrationEngine.Model;
using Moq;
using NUnit.Framework;

namespace IntegrationEngine.Tests.Api.Controllers
{
    public class CronTriggerControllerTest
    {
        [Test]
        public void ShouldScheduleJobWhenCronTriggerIsCreated()
        {
            var subject = new CronTriggerController();
            var cronExpression = "0 6 * * 1-5";
            var jobType = "MyProject.MyIntegrationJob";
            var expected = new CronTrigger() {
                JobType = jobType,
                CronExpressionString = cronExpression
            };
            var engineScheduler = new Mock<IEngineScheduler>();
            engineScheduler.Setup(x => x.ScheduleJobWithCronTrigger(expected));
            subject.EngineScheduler = engineScheduler.Object;

            subject.PostIntegrationJob(expected);

            engineScheduler.Verify(x => x
                .ScheduleJobWithCronTrigger(It.Is<CronTrigger>(y => y.JobType == jobType && 
                                                                    y.CronExpressionString == cronExpression)), 
                Times.Once);
        }
    }
}
