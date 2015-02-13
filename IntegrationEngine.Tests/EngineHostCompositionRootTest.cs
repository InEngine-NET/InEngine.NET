using BeekmanLabs.UnitTesting;
using Common.Logging;
using Common.Logging.NLog;
using IntegrationEngine.Api;
using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.R;
using IntegrationEngine.MessageQueue;
using Microsoft.Practices.Unity;
using Moq;
using Nest;
using NUnit.Framework;

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

            Assert.That(Subject.Log.GetType(), Is.EqualTo(typeof (NLogLogger)));
        }

        [Test]
        public void ShouldSetupMessageQueueListener()
        {
            Subject.LoadConfiguration();
            Subject.Container.RegisterType<IRabbitMQConfiguration, RabbitMQConfiguration>("DefaultRabbitMQ");


            Subject.SetupThreadedListenerManager();

            Subject.Container.Resolve<IThreadedListenerManager>();
        }

        [Test]
        public void ShouldSetupRScriptRunner()
        {
            Subject.Container.RegisterInstance<IElasticClient>(new ElasticClient());

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