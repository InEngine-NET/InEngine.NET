using BeekmanLabs.UnitTesting;
using IntegrationEngine.Api;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Scheduler;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Results;

namespace IntegrationEngine.Tests.Api
{
    public class TriggerControllerBaseTest : TestBase<TriggerControllerBase<TriggerStub>>
    {
        public Mock<IElasticsearchRepository> MockElasticRepo { get; set; }
        public Mock<IEngineScheduler> MockEngineScheduler { get; set; }
        public string TriggerDocumentId = "foo";

        [SetUp]
        public void Setup()
        {
            MockElasticRepo = new Mock<IElasticsearchRepository>();
            Subject.Repository = MockElasticRepo.Object;
            MockEngineScheduler = new Mock<IEngineScheduler>();
            Subject.EngineScheduler = MockEngineScheduler.Object;
        }

        [Test]
        public void ShouldGetListOfTriggers()
        {
            var expected = new List<TriggerStub>() { new TriggerStub(), new TriggerStub() };
            var expectedCount = expected.Count();
            MockElasticRepo.Setup(x => x.SelectAll<TriggerStub>()).Returns(expected);

            var actual = Subject.GetCollection();

            Assert.That(actual.Count(), Is.EqualTo(expectedCount));
            Assert.That(actual, Is.EquivalentTo(expected));
            MockElasticRepo.Verify(x => x.SelectAll<TriggerStub>(), Times.Once);
        }

        [Test]
        public void ShouldGetTriggerById()
        {
            var expected = new TriggerStub() { Id = TriggerDocumentId };
            MockElasticRepo.Setup(x => x.SelectById<TriggerStub>(TriggerDocumentId)).Returns(expected);

            var actual = Subject.Get(TriggerDocumentId);

            Assert.That(actual, Is.TypeOf(typeof(OkNegotiatedContentResult<TriggerStub>)));
            MockElasticRepo.Verify(x => x.SelectById<TriggerStub>(TriggerDocumentId), Times.Once);
        }

        [Test]
        public void ShouldReturnNotFoundIfTriggerDoesNotExist()
        {
            MockElasticRepo.Setup(x => x.SelectById<TriggerStub>(TriggerDocumentId));

            var actual = Subject.Get(TriggerDocumentId);

            Assert.That(actual, Is.TypeOf(typeof(NotFoundResult)));
        }

        [Test]
        public void ShouldUpdateAndReturnTrigger()
        {
            var expected = new TriggerStub() { Id = TriggerDocumentId };
            MockElasticRepo.Setup(x => x.Update(expected)).Returns(expected);
            MockEngineScheduler.Setup(x => x.ScheduleJobWithTrigger(expected));

            var actual = Subject.Put(TriggerDocumentId, expected);

            Assert.That(actual, Is.TypeOf(typeof(OkNegotiatedContentResult<TriggerStub>)));
            MockElasticRepo.Verify(x => x.Update(expected), Times.Once);
            MockEngineScheduler.Verify(x => x.ScheduleJobWithTrigger(expected), Times.Once);
        }

        [Test]
        public void ShouldReturnBadRequestIfIdsDoNotMatchWhenUpdatingTrigger()
        {
            var actual = Subject.Put("1", new TriggerStub() { Id = "2" });

            Assert.That(actual, Is.TypeOf(typeof(BadRequestResult)));
        }

        [Test]
        public void ShouldScheduleJobWhenTriggerIsCreated()
        {
            var jobType = "MyProject.MyIntegrationJob";
            var triggerWithoutId = new TriggerStub() { JobType = jobType };
            var expected = new TriggerStub() {
                JobType = jobType,
                Id = TriggerDocumentId,
            };
            MockElasticRepo.Setup(x => x.Insert(triggerWithoutId)).Returns(expected);
            MockEngineScheduler.Setup(x => x.ScheduleJobWithTrigger(expected));
            MockEngineScheduler.Setup(x => x.IsJobTypeRegistered(jobType)).Returns(true);

            Subject.Post(triggerWithoutId);

            MockElasticRepo.Verify(x => x.Insert(triggerWithoutId), Times.Once);
            MockEngineScheduler.Verify(x => x
                .ScheduleJobWithTrigger(It.Is<TriggerStub>(y => y.JobType == jobType && y.Id == TriggerDocumentId)), Times.Once);
        }

        [Test]
        public void ShouldDeleteTrigger()
        {
            var expected = new TriggerStub() { Id = TriggerDocumentId };
            MockEngineScheduler.Setup(x => x.DeleteTrigger(expected));
            MockElasticRepo.Setup(x => x.SelectById<TriggerStub>(TriggerDocumentId)).Returns(expected);
            MockElasticRepo.Setup(x => x.Delete<TriggerStub>(TriggerDocumentId));

            Subject.Delete(TriggerDocumentId);

            MockElasticRepo.Verify(x => x.SelectById<TriggerStub>(TriggerDocumentId), Times.Once);
            MockElasticRepo.Verify(x => x.Delete<TriggerStub>(TriggerDocumentId), Times.Once);
            MockEngineScheduler.Verify(x => x.DeleteTrigger(expected), Times.Once);
        }

        [Test]
        public void ShouldReturnNotFoundAndNotAttemptToDeleteTriggerIfItDoesNotExist()
        {
            MockElasticRepo.Setup(x => x.SelectById<TriggerStub>(TriggerDocumentId));

            var actual = Subject.Delete(TriggerDocumentId);

            Assert.That(actual, Is.TypeOf(typeof(NotFoundResult)));
            MockElasticRepo.Verify(x => x.Delete<TriggerStub>(TriggerDocumentId), Times.Never);
            MockEngineScheduler.Verify(x => x.DeleteTrigger(It.IsAny<TriggerStub>()), Times.Never);
        }

        [Test]
        public void ShouldValidateJobTypeAgainstSchedulersRegisteredJobs()
        {
            var jobTypeName = "MyIntegrationServer.MyIntegrationJob";
            MockEngineScheduler.Setup(x => x.IsJobTypeRegistered(jobTypeName)).Returns(true);

            var actual = Subject.IsValidJobType(jobTypeName);

            MockEngineScheduler.Verify(x => x.IsJobTypeRegistered(jobTypeName), Times.Once);
            Assert.That(actual, Is.True);
        }

        [Test]
        public void ShouldReturnTrueIfJobTypeNameIsNotValid()
        {
            var actual = Subject.IsValidJobType(null);

            MockEngineScheduler.Verify(x => x.IsJobTypeRegistered(It.IsAny<string>()), Times.Never);
            Assert.That(actual, Is.True);
        }
    }
}
