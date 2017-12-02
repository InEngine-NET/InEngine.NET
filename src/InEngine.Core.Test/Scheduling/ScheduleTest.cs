using System;
using BeekmanLabs.UnitTesting;
using InEngine.Commands;
using InEngine.Core.Commands;
using InEngine.Core.Scheduling;
using Moq;
using NUnit.Framework;
using Quartz;

namespace InEngine.Core.Test.Scheduling
{
    [TestFixture]
    public class ScheduleTest : TestBase<Schedule>
    {
        [SetUp]
        public void Setup()
        {
            InEngineSettings.BasePath = TestContext.CurrentContext.TestDirectory;
        }

        [Test]
        public void ShouldScheduleToRunCommandEverySecond()
        {
            var alwaysSucceed = new AlwaysSucceed();

            Subject.Job(alwaysSucceed).EverySecond();
        }

        [Test]
        public void ShouldScheduleToRunLambdaEverySecond()
        {
            var alwaysSucceed = new AlwaysSucceed();

            Subject.Job(() => { Console.WriteLine("Hello, world!"); }).EverySecond();
        }
    }
}
