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

        [SetUp]
        public void Setup()
        {
            Subject.Container = UnityContainer = new UnityContainer();
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
        public void ShouldSetupDatabaseAndRepository()
        {
            Subject.LoadConfiguration();

            Subject.SetupDatabaseRepository();

            Subject.Container.Resolve<IntegrationEngineContext>();
            Subject.Container.Resolve<IDatabaseRepository>();
        }

        [Test]
        public void ShouldSetupMailClient()
        {
            Subject.LoadConfiguration();
            Subject.SetupLogging();

            Subject.SetupMailClient();

            Subject.Container.Resolve<IMailClient>();
        }

        [Test]
        public void ShouldSetupElasticClientAndRepository()
        {
            Subject.LoadConfiguration();
            Subject.SetupLogging();

            Subject.SetupElasticClientAndRepository();

            Subject.Container.Resolve<IElasticClient>();
            Subject.Container.Resolve<IElasticsearchRepository>();
        }

        [Test]
        public void ShouldSetupMessageQueueListener()
        {
            Subject.LoadConfiguration();
            Subject.SetupLogging();
            Subject.SetupMailClient();
            Subject.SetupDatabaseRepository();
            Subject.SetupElasticClientAndRepository();

            Subject.SetupMessageQueueListener();

            Subject.Container.Resolve<IMessageQueueListener>();
        }

        [Test]
        public void ShouldSetupMessageQueueClient()
        {
            Subject.LoadConfiguration();
            Subject.SetupLogging();

            Subject.SetupMessageQueueClient();

            Subject.Container.Resolve<IMessageQueueClient>();
        }

        [Test]
        public void ShouldSetupEngineScheduler()
        {
            Subject.IntegrationJobTypes = new List<Type>();
            Subject.LoadConfiguration();
            Subject.SetupLogging();
            Subject.SetupMessageQueueClient();
            Subject.SetupElasticClientAndRepository();

            Subject.SetupEngineScheduler();

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
