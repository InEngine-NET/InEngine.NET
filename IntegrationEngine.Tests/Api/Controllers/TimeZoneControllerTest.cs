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

namespace IntegrationEngine.Tests.Api.Controllers
{
    public class TimeZoneControllerTest : TestBase<TimeZoneController>
    {
        [Test]
        public void ShouldReturnListOfTimeZones()
        {
            var result = Subject.GetTimeZones().ToList();

            Assert.That(result.Count, Is.GreaterThan(0));
        }
    }
}

