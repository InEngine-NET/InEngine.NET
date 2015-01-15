using IntegrationEngine.Configuration;
using Microsoft.Practices.Unity;
using Moq;
using NUnit.Framework;
using System;

namespace IntegrationEngine.Tests
{
    public class EngineHostConfigurationTest
    {
        [Test]
        public void CanLoadConfiguration()
        {
            var subject = new EngineHostConfiguration();
            var container = new Mock<StubContainer>();
            subject.Container = container.Object;

            subject.LoadConfiguration();

            Assert.IsNotNull(subject.Configuration);
        }
    }
}
