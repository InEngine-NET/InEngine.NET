using BeekmanLabs.UnitTesting;
using Common.Logging;
using Common.Logging.NLog;
using IntegrationEngine.Api;
using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.R;
using IntegrationEngine.JobProcessor;
using Microsoft.Practices.Unity;
using Moq;
using Nest;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IntegrationEngine.Core.IntegrationJob;
using System.Threading.Tasks;

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
            var configName = "DefaultRabbitMQ";
            Subject.Container.RegisterType<IRabbitMQConfiguration, RabbitMQConfiguration>(configName,
                new InjectionConstructor(new ResolvedParameter<IEngineConfiguration>(), configName));

            Subject.SetupMessageQueueListenerManager();

            Assert.That(Subject.MessageQueueListenerManager, Is.Not.Null);
        }

        [Test]
        public void ShouldStartMessageQueueListener()
        {
            var mockMessageQueueListenerManager = new Mock<IMessageQueueListenerManager>();
            mockMessageQueueListenerManager.Setup(x => x.StartListener())
                .Returns(() => Task.FromResult(default(Task)));
            Subject.MessageQueueListenerManager = mockMessageQueueListenerManager.Object;

            Subject.StartMessageQueueListener();

            mockMessageQueueListenerManager.Verify(x => x.StartListener(), Times.Once);
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

        [Test]
        public void ShouldRegisterIntegrationPoints()
        {
            Subject.EngineConfiguration = new EngineConfiguration();
            Subject.LoadConfiguration();

            Subject.RegisterIntegrationPoints();

            var container = Subject.Container;
            container.Resolve<IMailConfiguration>("DefaultMail");
            container.Resolve<IMailConfiguration>("FooMailClient");
            container.Resolve<IElasticsearchConfiguration>("DefaultElasticsearch");
            container.Resolve<IRabbitMQConfiguration>("DefaultRabbitMQ");
            container.Resolve<IJsonServiceConfiguration>("ExampleJsonService");
        }

        [Test]
        public void ShouldRegisterIntegrationJobs()
        {
            var assembliesWithJobs = new List<Assembly> { typeof(EngineHostCompositionRootTest).Assembly };
            Subject.IntegrationJobTypes = Subject.ExtractIntegrationJobTypesFromAssemblies(assembliesWithJobs);
            Subject.LoadConfiguration();
            Subject.RegisterIntegrationPoints();

            Subject.RegisterIntegrationJobs();

            Assert.That(Subject.Container.IsRegistered<IntegrationJobStub>(), Is.True);
        }
    }
}