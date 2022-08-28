using System;
using System.Linq.Expressions;
using InEngine.Commands;
using InEngine.Core.Commands;
using InEngine.Core.Queuing;
using Moq;
using NUnit.Framework;
using Serialize.Linq.Extensions;

namespace InEngine.Core.Test.Queuing;

[TestFixture]
public class QueueAdapterTest : TestBase<QueueAdapter>
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
        Expression<Action> expression = () => Console.Write("Hello, world.");
        var lambda = new Lambda(expression.ToExpressionNode());
        MockQueueClient.Setup(x => x.Publish(It.IsAny<Lambda>()));

        Subject.Publish(lambda);

        MockQueueClient.Verify(x => x.Publish(It.IsAny<Lambda>()), Times.Once());
    }
}