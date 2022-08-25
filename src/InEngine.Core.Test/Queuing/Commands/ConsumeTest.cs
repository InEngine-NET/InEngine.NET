using BeekmanLabs.UnitTesting;
using InEngine.Commands;
using InEngine.Core.Queuing.Commands;
using Moq;
using NUnit.Framework;
using Quartz;

namespace InEngine.Core.Test.Queuing.Commands
{
    [TestFixture]
    public class ConsumeTest : TestBase<Consume>
    {
        [SetUp]
        public void Setup() => InEngineSettings.BasePath = TestContext.CurrentContext.TestDirectory;
    }
}
