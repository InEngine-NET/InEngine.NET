using System.Collections.Generic;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using System.Threading.Tasks;
using InEngineTesting;
using Moq;

namespace InEngine.Core.Test.Commands;

[TestFixture]
public class ChainTest : TestBase<Chain>
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task ShouldRunChainOfCommands()
    {
        var mockCommand1 = new Mock<AbstractCommand>();
        var mockCommand2 = new Mock<AbstractCommand>();
        var commands = new[]
        {
            mockCommand1.Object,
            mockCommand2.Object,
        };
        Subject.Commands = commands;

        await Subject.RunAsync();

        mockCommand1.Verify(x => x.RunAsync(), Times.Once());
        mockCommand2.Verify(x => x.RunAsync(), Times.Once());
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

        Assert.ThrowsAsync<CommandChainFailedException>(async () => await Subject.RunAsync());

        mockCommand1.Verify(x => x.RunAsync(), Times.Once());
        mockCommand2.Verify(x => x.RunAsync(), Times.Never());
    }

    [Test]
    public async Task ShouldRunChainOfDifferentCommands()
    {
        Subject.Commands = new List<AbstractCommand>
        {
            new AlwaysSucceed(),
            new Echo { VerbatimText = "Hello, world!" },
        };

        await Subject.RunAsync();
    }

    [Test]
    public async Task ShouldRunChainOfDifferentCommandsAsAbstractCommand()
    {
        Subject.Commands = new AbstractCommand[]
        {
            new AlwaysSucceed(),
            new Echo(verbatimText: "Hello, world!"),
        };

        await Subject.RunAsync();
    }
}