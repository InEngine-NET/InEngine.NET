using System;
using System.Collections.Generic;
using System.Linq;
using BeekmanLabs.UnitTesting;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using InEngine.Core.Queue.Commands;
using NUnit.Framework;

namespace InEngine.Core.Test.Queue.Commands
{
    [TestFixture]
    public class PublishTest : TestBase<Publish>
    {
        [SetUp]
        public void Setup()
        {
            InEngineSettings.BasePath = TestContext.CurrentContext.TestDirectory;
        }

        [Test]
        public void ShouldPublishCommandObject()
        {
            Subject.Command = new AlwaysSucceed();

            Subject.Run();
        }

        [Test]
        public void ShouldPublishCommandByArgs()
        {
            var nullCommand = new AlwaysSucceed();
            Subject.CommandAssembly = nullCommand.GetType().Assembly.GetName().Name + ".dll";
            Subject.CommandClass = nullCommand.GetType().FullName;

            Subject.Run();
        }

        [Test]
        public void ShouldPublishManyCommands()
        {

            foreach (var i in Enumerable.Range(0, 200).ToList())
            {
                Subject.Command = new Echo()
                {
                    VerbatimText = $"test job: {i}"
                };
                Subject.Run();
            }
        }

        [Test]
        public void ShouldFailWhenCommandDoesNotExist()
        {
            Subject.CommandAssembly = "foo";
            Subject.CommandClass = "bar";
            var expectedMessage = "Plugin not found at ";

            var result = Assert.Throws<PluginNotFoundException>(() => { Subject.Run(); });

            Assert.IsTrue(result.Message.StartsWith(expectedMessage, StringComparison.InvariantCulture));
        }
    }
}
