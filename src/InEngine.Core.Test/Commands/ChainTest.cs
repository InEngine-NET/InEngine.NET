using System.Collections.Generic;
using BeekmanLabs.UnitTesting;
using InEngine.Commands;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using Moq;
using NUnit.Framework;
using Quartz;

namespace InEngine.Core.Test.Commands;

[TestFixture]
public class ChainTest : TestBase<Chain>
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ShouldRunChainOfCommands()
    {
        var mockCommand1 = new Mock<AbstractCommand>();
        var mockCommand2 = new Mock<AbstractCommand>();
        var commands = new[]
        {
            mockCommand1.Object,
            mockCommand2.Object,
        };
        Subject.Commands = commands;

        Subject.Run();

        mockCommand1.Verify(x => x.Run(), Times.Once());
        mockCommand2.Verify(x => x.Run(), Times.Once());
    }

    [Test]
    public void ShouldRunChainOfCommandsAndFail()
    {
        var mockCommand1 = new Mock<AlwaysFail>();
        var mockCommand2 = new Mock<AlwaysFail>();
        var alwaysFail = new AlwaysFail();
        var commands = new AbstractCommand[]
        {
            mockCommand1.Object,
            alwaysFail,
            mockCommand2.Object,
        };
        Subject.Commands = commands;

        Assert.That(Subject.Run, Throws.TypeOf<CommandChainFailedException>());

        mockCommand1.Verify(x => x.Run(), Times.Once());
        mockCommand2.Verify(x => x.Run(), Times.Never());
    }

    [Test]
    public void ShouldRunChainOfDifferentCommands()
    {
        Subject.Commands = new List<AbstractCommand>
        {
            new AlwaysSucceed(),
            new Echo() { VerbatimText = "Hello, world!" },
        };

        Subject.Run();
    }

    [Test]
    public void ShouldRunChainOfDifferentCommandsAsAbstractCommand()
    {
        Subject.Commands = new AbstractCommand[]
        {
            new AlwaysSucceed(),
            new Echo(verbatimText: "Hello, world!"),
        };

        Subject.Run();
    }
}