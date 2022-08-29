using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using InEngineTesting;

namespace InEngine.Core.Test.Commands;

public class AlwaysFailTest : TestBase<AlwaysFail>
{
    [Test]
    public void ShouldFailWithException()
    {
        Assert.ThrowsAsync<CommandFailedException>(async () => await Subject.RunAsync());
    }
    
    [Test]
    public void ShouldFailWithExceptionWhenRunWithLifeCycleMethods()
    {
        Assert.ThrowsAsync<CommandFailedException>(async () => await Subject.RunWithLifeCycleAsync());
    }
}