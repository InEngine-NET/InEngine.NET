using BeekmanLabs.UnitTesting;
using Microsoft.Practices.Unity;
using Moq;
using NUnit.Framework;

namespace IntegrationEngine.Tests
{
    public class EngineHostConfigurationTest : TestBase<EngineHostConfiguration>
    {
        [Test]
        public void CanLoadConfiguration()
        {
            var container = new Mock<IUnityContainer>();
            Subject.Container = container.Object;

            Subject.LoadConfiguration();

            Assert.IsNotNull(Subject.Configuration);
        }
    }
}
