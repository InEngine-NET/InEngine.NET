using System;
using System.Linq;
using BeekmanLabs.UnitTesting;
using InEngine.Commands;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using InEngine.Core.Queuing.Commands;
using NUnit.Framework;

namespace InEngine.Core.Test.Queuing.Commands
{
    [TestFixture]
    public class PublishTest : TestBase<Publish>
    {
        [SetUp]
        public void Setup()
        {
            InEngineSettings.BasePath = TestContext.CurrentContext.TestDirectory;
        }
    }
}
