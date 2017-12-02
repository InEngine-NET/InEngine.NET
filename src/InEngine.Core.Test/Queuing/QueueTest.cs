using System;
using BeekmanLabs.UnitTesting;
using InEngine.Commands;
using InEngine.Core.Commands;
using InEngine.Core.Queuing;
using InEngine.Core.Queuing.Clients;
using InEngine.Core.Queuing.Commands;
using Moq;
using NUnit.Framework;
using Quartz;

namespace InEngine.Core.Test.Queuing
{
    [TestFixture]
    public class QueueTest : TestBase<Queue>
    {
        public Mock<IQueueClient> MockQueueClient { get; set; }

        [SetUp]
        public void Setup()
        {
            InEngineSettings.BasePath = TestContext.CurrentContext.TestDirectory;
            MockQueueClient = new Mock<IQueueClient>();
            Subject.QueueClient = MockQueueClient.Object;
        }

        [Test]
        public void ShouldPublishCommand()
        {
            var command = new AlwaysSucceed();
            MockQueueClient.Setup(x => x.Publish(command));

            Subject.Publish(command);

            MockQueueClient.Verify(x => x.Publish(command), Times.Once());
        }

        [Test]
        public void ShouldPublishLambdaCommand()
        {
            Action action = () => { Console.Write("Hello, world."); };
            var lambda = new Lambda() { Action = action };
            MockQueueClient.Setup(x => x.Publish(It.IsAny<Lambda>()));

            Subject.Publish(action);

            MockQueueClient.Verify(x => x.Publish(It.Is<Lambda>(y => y.Action == action)), Times.Once());
        }

        [Test]
        public void ShouldPublishChainOfCommands()
        {
            var commands = new[] {
                new AlwaysSucceed(),
                new AlwaysSucceed(),
                new AlwaysSucceed(),
                new AlwaysSucceed(),
            };
            MockQueueClient.Setup(x => x.Publish(It.IsAny<Chain>()));

            Subject.Publish(commands);

            MockQueueClient.Verify(x => x.Publish(It.Is<Chain>(y => y.Commands.Equals(commands))), Times.Once());
        }
    }
}
