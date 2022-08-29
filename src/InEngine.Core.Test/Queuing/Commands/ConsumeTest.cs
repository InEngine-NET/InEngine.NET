using InEngine.Commands;
using InEngine.Core.Queuing.Commands;
using Moq;
using Quartz;

namespace InEngine.Core.Test.Queuing.Commands;

using InEngineTesting;

[TestFixture]
public class ConsumeTest : TestBase<Consume>
{
    [SetUp]
    public void Setup() => InEngineSettings.BasePath = TestContext.CurrentContext.TestDirectory;
}