using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IntegrationEngine.Tests
{
    [TestClass]
    public class EngineHostConfigurationTest
    {
        [TestMethod]
        public void CanLoadConfiguration()
        {
            var subject = new EngineHostConfiguration();
            
            subject.LoadConfiguration();

            Assert.IsNotNull(subject.Configuration);
        }
    }
}
