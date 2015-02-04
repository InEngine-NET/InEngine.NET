using BeekmanLabs.UnitTesting;
using Common.Logging;
using IntegrationEngine.Api;
using IntegrationEngine.Core.Mail;
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
    public class EngineHostConfigurationTest : TestBase<EngineHostConfiguration>
    {
        public IUnityContainer UnityContainer { get; set; }
        public Mock<ILog> MockLog { get; set; }

        [SetUp]
        public void Setup()
        {
            Subject.Container = UnityContainer = new UnityContainer();
            MockLog = new Mock<ILog>();
        }

        [Test]
        public void CanLoadConfiguration()
        {
            Subject.LoadConfiguration();

            Assert.IsNotNull(Subject.Configuration);
        }

        [Test]
        public void ShouldSetupLogging()
        {
            Subject.LoadConfiguration();

            Subject.SetupLogging();

            Subject.Container.Resolve<ILog>();
        }

        [Test]
        public void ShouldSetupDatabaseContext()
        {
            Subject.LoadConfiguration();

            Subject.SetupDatabaseContext();

            Subject.Container.Resolve<IntegrationEngineContext>();
        }

        [Test]
        public void ShouldSetupDatabaseRepository()
        {
            Subject.LoadConfiguration();
            var mockIntegrationEngineContext = new Mock<IntegrationEngineContext>();

            Subject.SetupDatabaseRepository(mockIntegrationEngineContext.Object);

            Subject.Container.Resolve<IDatabaseRepository>();
        }

        [Test]
        public void ShouldSetupMailClient()
        {
            Subject.LoadConfiguration();

            Subject.SetupMailClient(MockLog.Object);

            Subject.Container.Resolve<IMailClient>();
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

            Subject.SetupElasticsearchRepository(MockLog.Object, mockElasticClient.Object);

            Subject.Container.Resolve<IElasticsearchRepository>();
        }

        [Test]
        public void ShouldSetupMessageQueueListener()
        {
            Subject.LoadConfiguration();
            var mockMailClient = new Mock<IMailClient>();
            var mockElasticClient = new Mock<IElasticClient>();
            var mockIntegrationEngineContext = new Mock<IntegrationEngineContext>();

            Subject.SetupMessageQueueListener(MockLog.Object, mockMailClient.Object, mockElasticClient.Object, mockIntegrationEngineContext.Object);

            Subject.Container.Resolve<IMessageQueueListener>();
        }

        [Test]
        public void ShouldSetupMessageQueueClient()
        {
            Subject.LoadConfiguration();

            Subject.SetupMessageQueueClient(MockLog.Object);

            Subject.Container.Resolve<IMessageQueueClient>();
        }

        [Test]
        public void ShouldSetupEngineScheduler()
        {
            Subject.IntegrationJobTypes = new List<Type>();
            Subject.LoadConfiguration();
            var mockMailClient = new Mock<IMailClient>();
            var mockElasticsearchRespository= new Mock<IElasticsearchRepository>();
            var mockMessageQueueClient = new Mock<IMessageQueueClient>();

            Subject.SetupEngineScheduler(MockLog.Object, mockMessageQueueClient.Object, mockElasticsearchRespository.Object);

            Subject.Container.Resolve<IEngineScheduler>();
        }

        [Test]
        public void ShouldSetupRScriptRunner()
        {
            Subject.SetupRScriptRunner();

            Subject.Container.Resolve<RScriptRunner>();
        }

        [Test]
        public void ShouldShutdownSchedulerAndDisposeOfMessageQueueListener()
        {
            var mockEngineScheduler = new Mock<IEngineScheduler>();
            mockEngineScheduler.Setup(x => x.Shutdown());
            UnityContainer.RegisterInstance<IEngineScheduler>(mockEngineScheduler.Object);
            var mockMessageQueueListener = new Mock<IMessageQueueListener>();
            mockMessageQueueListener.Setup(x => x.Dispose());
            UnityContainer.RegisterInstance<IMessageQueueListener>(mockMessageQueueListener.Object);

            Subject.Dispose();

            mockEngineScheduler.Verify(x => x.Shutdown(), Times.Once);
            mockMessageQueueListener.Verify(x => x.Dispose(), Times.Once);
        }
    }
}
