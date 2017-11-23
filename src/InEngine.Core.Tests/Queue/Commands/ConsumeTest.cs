using System.Collections.Generic;
using System.Linq;
using BeekmanLabs.UnitTesting;
using InEngine.Core.Commands;
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
            new Publish() {
                Command = new AlwaysSucceed()
            }.Run();

            Subject.Run();
        }

        [Test]
        public void ShouldConsumeSecondaryQueue()
        {
            var expectedMessage = "Consumed";
            new Publish() {
                Command = new AlwaysSucceed()
            }.Run();
            Subject.UseSecondaryQueue = true;

            Subject.Run();
        }

        [Test]
        public void ShouldConsumeSecondaryQueueBasedOnJobContextData()
        {
            new Publish() {
                Command = new AlwaysSucceed()
            }.Run();
            var jobExecutionConext = new Mock<IJobExecutionContext>();
            var jobDataMap = new JobDataMap { { "useSecondaryQueue", true } };
            jobExecutionConext.SetupGet(p => p.JobDetail.JobDataMap).Returns(jobDataMap);
            Subject.JobExecutionContext = jobExecutionConext.Object;

            Subject.Run();
        }
    }
}
