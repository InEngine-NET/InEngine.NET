using System;
using BeekmanLabs.UnitTesting;
using InEngine.Commands;
using InEngine.Core.Commands;
using InEngine.Core.Queuing;
using InEngine.Core.Queuing.Commands;
using Moq;
using NUnit.Framework;
using Quartz;

namespace InEngine.Core.Test.Queuing
{
    [TestFixture]
    public class QueueTest : TestBase
    {
        public Queue Subject { get; private set; }

        [SetUp]
        public void Setup()
        {
            InEngineSettings.BasePath = TestContext.CurrentContext.TestDirectory;
            Subject = Queue.Make();
        }

        [Test]
        public void ShouldPublishLambda()
        {
            Subject.Publish(() => { Console.Write("Hello, world."); });
        }
    }
}
