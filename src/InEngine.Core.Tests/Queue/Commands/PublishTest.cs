using System;
using System.Collections.Generic;
using System.Linq;
using BeekmanLabs.UnitTesting;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using InEngine.Core.Queue.Commands;
using NUnit.Framework;

namespace InEngine.Core.Tests.Queue.Commands
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
            var expectedMessage = "Published";
            Subject.Command = new Null();

            var result = Subject.Run();

            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(expectedMessage, result.Message);
        }

        [Test]
        public void ShouldPublishCommandByArgs()
        {
            var expectedMessage = "Published";
            var nullCommand = new Null();
            Subject.CommandAssembly = nullCommand.GetType().Assembly.GetName().Name + ".dll";
            Subject.CommandClass = nullCommand.GetType().FullName;

            var result = Subject.Run();

            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(expectedMessage, result.Message);
        }

        [Test]
        public void ShouldPublishManyCommands()
        {
            //var expectedMessage = "Published";
            Subject.Command = new Null();
            var results = new List<CommandResult>();

            foreach(var i in Enumerable.Range(0, 200).ToList())
                results.Add(Subject.Run());

            //Assert.IsTrue(result.IsSuccessful);
            //Assert.AreEqual(expectedMessage, result.Message);
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
