using BeekmanLabs.UnitTesting;
using IntegrationEngine.Api.Controllers;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Scheduler;
using Moq;
using NUnit.Framework;

namespace IntegrationEngine.Tests.Api.Controllers
{
    public class SimpleTriggerControllerTest : TestBase<SimpleTriggerController>
    {
        [Test]
        public void ShouldScheduleSimpleJob()
        {
            var jobType = "MyProject.MyIntegrationJob";
            var expected = new SimpleTrigger() {
                JobType = jobType
            };
            var engineScheduler = new Mock<IEngineScheduler>();
            engineScheduler.Setup(x => x.ScheduleJobWithTrigger(expected));
            Subject.EngineScheduler = engineScheduler.Object;
            var esRepository = new Mock<IElasticsearchRepository>();
            esRepository.Setup(x => x.Insert<SimpleTrigger>(expected)).Returns(expected);
            Subject.Repository = esRepository.Object;

            Subject.Post(expected);

            engineScheduler.Verify(x => x.ScheduleJobWithTrigger(It.Is<SimpleTrigger>(y => y.JobType == jobType)), Times.Once);
        }

        [Test]
        public void ShouldDeleteSimpleTrigger()
        {
            string id = "1";
            var expected = new SimpleTrigger() { Id = id };
            var engineScheduler = new Mock<IEngineScheduler>();
            engineScheduler.Setup(x => x.DeleteTrigger(expected));
            Subject.EngineScheduler = engineScheduler.Object;
            var esRepository = new Mock<IElasticsearchRepository>();
            esRepository.Setup(x => x.SelectById<SimpleTrigger>(id)).Returns(expected);
            esRepository.Setup(x => x.Delete<SimpleTrigger>(id));
            Subject.Repository = esRepository.Object;

            Subject.Delete(id);

            esRepository.Verify(x => x.SelectById<SimpleTrigger>(id), Times.Once);
            esRepository.Verify(x => x.Delete<SimpleTrigger>(id), Times.Once);
            engineScheduler.Verify(x => x.DeleteTrigger(expected), Times.Once);
        }
    }
}
