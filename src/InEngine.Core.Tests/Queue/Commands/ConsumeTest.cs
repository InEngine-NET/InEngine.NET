using System.Collections.Generic;
using System.Linq;
using BeekmanLabs.UnitTesting;
using InEngine.Core.Exceptions;
using InEngine.Core.Queue.Commands;
using Moq;
using NUnit.Framework;
using Quartz;

namespace InEngine.Core.Tests.Queue.Commands
{
    [TestFixture]
    public class ConsumeTest : TestBase<Consume>
    {
        [SetUp]
        public void Setup()
        {
            InEngineSettings.BasePath = TestContext.CurrentContext.TestDirectory;
        }

        [Test]
        public void ShouldConsumePrimaryQueue()
        {
            var expectedMessage = "Consumed";
            new Publish() {
                Command = new Null()
            }.Run();

            var result = Subject.Run();

            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(expectedMessage, result.Message);
        }

        [Test]
        public void ShouldConsumeSecondaryQueue()
        {
            var expectedMessage = "Consumed";
            new Publish() {
                Command = new Null()
            }.Run();
            Subject.UseSecondaryQueue = true;

            var result = Subject.Run();

            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(expectedMessage, result.Message);
        }

        [Test]
        public void ShouldConsumeSecondaryQueueBasedOnJobContextData()
        {
            var expectedMessage = "Consumed";
            new Publish() {
                Command = new Null()
            }.Run();
            var jobExecutionConext = new Mock<IJobExecutionContext>();
            var jobDataMap = new JobDataMap { { "useSecondaryQueue", true } };
            jobExecutionConext.SetupGet(p => p.JobDetail.JobDataMap).Returns(jobDataMap);
            Subject.JobExecutionContext = jobExecutionConext.Object;

            var result = Subject.Run();

            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(expectedMessage, result.Message);
        }
    }
}
