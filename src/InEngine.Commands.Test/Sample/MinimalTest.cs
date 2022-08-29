using InEngine.Commands.Sample;
using InEngine.Core.IO;
using InEngineTesting;
using Moq;

namespace InEngine.Commands.Test.Sample;

public class MinimalTest : TestBase<Minimal>
{
    [Test]
    public async Task ShouldSucceed()
    {
        const string expected = "This is an example of a minimal command.";
        var mockWrite = new Mock<IConsoleWrite>();
        Subject.Write = mockWrite.Object;

        await Subject.RunAsync();

        mockWrite.Verify(x => x.Info(expected), Times.Once());
        Assert.Pass();
    }
}