﻿using System;
using BeekmanLabs.UnitTesting;
using InEngine.Commands;
using InEngine.Core.Scheduling;
using NUnit.Framework;

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

        [Test]
        public void ShouldScheduleCommandChain()
        {
            var alwaysSucceed = new AlwaysSucceed();

            Subject.Job(new [] {
                new AlwaysSucceed(),
                new AlwaysSucceed(),
                new AlwaysSucceed(),
                new AlwaysSucceed()
            }).EverySecond();
        }
    }
}
