using System.Threading.Tasks;
using InEngine.Core.Commands;
using InEngine.Core.IO;
using InEngineTesting;
using Moq;

namespace InEngine.Core.Test.Commands;

public class AlwaysSucceedTest : TestBase<AlwaysSucceed>
{
    [Test]
    public async Task ShouldSucceed()
    {
        const string expected = "This command always succeeds.";
        var mockWrite = new Mock<IConsoleWrite>();
        Subject.Write = mockWrite.Object;

        await Subject.RunAsync();

        mockWrite.Verify(x => x.Info(expected), Times.Once());
        Assert.Pass();
    }
}