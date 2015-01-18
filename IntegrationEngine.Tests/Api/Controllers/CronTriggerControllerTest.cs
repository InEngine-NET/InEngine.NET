using IntegrationEngine.Api.Controllers;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Model;
using IntegrationEngine.Scheduler;
using Moq;
using NUnit.Framework;

namespace IntegrationEngine.Tests.Api.Controllers
{
    public class CronTriggerControllerTest
    {
        [Test]
        public void ShouldScheduleJobWhenCronTriggerIsCreatedWithValidCronExpression()
        {
            var subject = new CronTriggerController();
            var cronExpression = "0 6 * * 1-5 ?";
            var jobType = "MyProject.MyIntegrationJob";
            var expected = new CronTrigger() {
                JobType = jobType,
                CronExpressionString = cronExpression
            };
            var engineScheduler = new Mock<IEngineScheduler>();
            engineScheduler.Setup(x => x.ScheduleJobWithCronTrigger(expected));
            engineScheduler.Setup(x => x.IsJobTypeRegistered(expected.JobType)).Returns(true);
            subject.EngineScheduler = engineScheduler.Object;
            var esRepository = new Mock<ESRepository<CronTrigger>>();
            esRepository.Setup(x => x.Insert(expected)).Returns(expected);
            subject.Repository = esRepository.Object;

            subject.PostCronTrigger(expected);

            engineScheduler.Verify(x => x
                .ScheduleJobWithCronTrigger(It.Is<CronTrigger>(y => y.JobType == jobType &&
                                                                    y.CronExpressionString == cronExpression)), Times.Once);
        }
    }
}
