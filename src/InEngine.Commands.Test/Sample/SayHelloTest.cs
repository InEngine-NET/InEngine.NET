using InEngine.Commands.Sample;
using InEngine.Core.IO;
using InEngineTesting;
using Moq;

namespace InEngine.Commands.Test.Sample;

public class SayHelloTest : TestBase<SayHello>
{
    [Test]
    public async Task ShouldSayHelloTest()
    {
        const string expected = "hello";
        var mockWrite = new Mock<IConsoleWrite>();
        Subject.Write = mockWrite.Object;

        await Subject.RunAsync();

        mockWrite.Verify(x => x.Info(expected), Times.Once());
        Assert.Pass();
    }
}