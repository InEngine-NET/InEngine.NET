using BeekmanLabs.UnitTesting;
using IntegrationEngine.Api.Controllers;
using IntegrationEngine.Model;
using IntegrationEngine.Scheduler;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace IntegrationEngine.Tests
{
    public class JobTypeControllerTest : TestBase<JobTypeController>
    {
        [Test]
        public void ShouldReturnListOfJobTypes()
        {
            var engineScheduler = new Mock<EngineScheduler>();
            var type = typeof(IntegrationJobStub);
            var expected = new JobType() {
                FullName = type.FullName,
                Name = type.Name,
            };
            engineScheduler.SetupGet(x => x.IntegrationJobTypes).Returns(new List<Type>() { type });
            Subject.EngineScheduler = engineScheduler.Object;

            var result = Subject.GetJobTypes();

            var first = result.First();
            Assert.That(first.FullName, Is.EqualTo(expected.FullName));
            Assert.That(first.Name, Is.EqualTo(expected.Name));
        }
    }
}

