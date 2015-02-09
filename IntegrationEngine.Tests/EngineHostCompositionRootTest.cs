using BeekmanLabs.UnitTesting;
using Common.Logging;
using IntegrationEngine.Api;
using IntegrationEngine.Core.MessageQueue;
using IntegrationEngine.Core.R;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.MessageQueue;
using IntegrationEngine.Scheduler;
using Microsoft.Practices.Unity;
using Moq;
using Nest;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace IntegrationEngine.Tests
{
    public class EngineHostCompositionRootTest : TestBase<EngineHostCompositionRoot>
    {
        public IUnityContainer UnityContainer { get; set; }
        public Mock<ILog> MockLog { get; set; }

        [SetUp]
        public void Setup()
        {
            Subject.Container = UnityContainer = new UnityContainer();
            MockLog = new Mock<ILog>();
            Subject.Log = MockLog.Object;
        }

        [Test]
        public void CanLoadConfiguration()
        {
            Subject.LoadConfiguration();

            Assert.IsNotNull(Subject.EngineConfiguration);
        }

        [Test]
        public void ShouldSetupLogging()
        {
            Subject.LoadConfiguration();

            Subject.SetupLogging();

            Assert.That(Subject.Log.GetType(), Is.EqualTo(typeof(Common.Logging.NLog.NLogLogger)));
        }

        [Test]
        public void ShouldSetupElasticClient()
        {
            Subject.LoadConfiguration();

            Subject.SetupElasticClient();

            Subject.Container.Resolve<IElasticClient>();
        }

        [Test]
        public void ShouldSetupElasticsearchRepository()
        {
            Subject.LoadConfiguration();
            var mockElasticClient = new Mock<IElasticClient>();

            Subject.SetupElasticsearchRepository(mockElasticClient.Object);

            Subject.Container.Resolve<IElasticsearchRepository>();
        }

        [Test]
        public void ShouldSetupMessageQueueListener()
        {
            Subject.LoadConfiguration();

            Subject.SetupThreadedListenerManager();

            Subject.Container.Resolve<IThreadedListenerManager>();
        }

        [Test]
        public void ShouldSetupMessageQueueClient()
        {
            Subject.LoadConfiguration();

            Subject.SetupMessageQueueClient();

            Subject.Container.Resolve<IMessageQueueClient>();
        }

        [Test]
        public void ShouldSetupEngineScheduler()
        {
            Subject.IntegrationJobTypes = new List<Type>();
            Subject.LoadConfiguration();
            var mockElasticsearchRespository= new Mock<IElasticsearchRepository>();
            var mockDispatcher = new Mock<IDispatcher>();

            Subject.SetupEngineScheduler(mockDispatcher.Object, mockElasticsearchRespository.Object);

            Subject.Container.Resolve<IEngineScheduler>();
        }

        [Test]
        public void ShouldSetupRScriptRunner()
        {
            Subject.SetupRScriptRunner();

            Subject.Container.Resolve<RScriptRunner>();
        }

        [Test]
        public void ShouldDisposeOfResources()
        {
            var mockWebApiApplication = new Mock<IWebApiApplication>();
            Subject.WebApiApplication = mockWebApiApplication.Object;
            
            Subject.Dispose();

            mockWebApiApplication.Verify(x => x.Dispose(), Times.Once);
        }
    }
}
