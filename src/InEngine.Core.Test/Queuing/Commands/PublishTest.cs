using System;
using System.Linq;
using InEngine.Commands;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using InEngineTesting;
using InEngine.Core.Queuing.Commands;

namespace InEngine.Core.Test.Queuing.Commands;

[TestFixture]
public class PublishTest : TestBase<Publish>
{
    [SetUp]
    public void Setup() => InEngineSettings.BasePath = TestContext.CurrentContext.TestDirectory;
}