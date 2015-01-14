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
            
            subject.LoadConfiguration();

            Assert.IsNotNull(subject.Configuration);
        }
    }
}
