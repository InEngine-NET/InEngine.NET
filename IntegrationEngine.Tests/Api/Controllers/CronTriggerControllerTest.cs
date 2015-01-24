using BeekmanLabs.UnitTesting;
using IntegrationEngine.Api.Controllers;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Scheduler;
using Moq;
using NUnit.Framework;

namespace IntegrationEngine.Tests.Api.Controllers
{
    public class CronTriggerControllerTest : TestBase<CronTriggerController>
    {
        [Test]
        public void ShouldScheduleJobWhenCronTriggerIsCreatedWithValidCronExpression()
        {
            var cronExpression = "0 6 * * 1-5 ?";
            var jobType = "MyProject.MyIntegrationJob";
            var expected = new CronTrigger() {
                JobType = jobType,
                CronExpressionString = cronExpression
            };
            var engineScheduler = new Mock<IEngineScheduler>();
            engineScheduler.Setup(x => x.ScheduleJobWithCronTrigger(expected));
            Subject.EngineScheduler = engineScheduler.Object;
            var esRepository = new Mock<ESRepository<CronTrigger>>();
            esRepository.Setup(x => x.Insert(expected)).Returns(expected);
            Subject.Repository = esRepository.Object;

            Subject.PostCronTrigger(expected);

            engineScheduler.Verify(x => x
                .ScheduleJobWithCronTrigger(It.Is<CronTrigger>(y => y.JobType == jobType &&
                                                                    y.CronExpressionString == cronExpression)), Times.Once);
        }
    }
}
