using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.NLog;
using IntegrationEngine.Api;
using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.Elasticsearch;
using IntegrationEngine.Core.IntegrationPoint;
using IntegrationEngine.Core.IntegrationJob;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.MessageQueue;
using IntegrationEngine.Core.R;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.JobProcessor;
using IntegrationEngine.Scheduler;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;
using Nest;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using IntegrationEngine.Core.ServiceStack;

namespace IntegrationEngine
{
    public class EngineHostCompositionRoot : IDisposable
    {
        public IUnityContainer Container { get; set; }
        public IEngineConfiguration EngineConfiguration { get; set; }
        public IList<Type> IntegrationJobTypes { get; set; }
        public ILog Log { get; set; }
        public IWebApiApplication WebApiApplication { get; set; }
        public IMessageQueueListenerManager MessageQueueListenerManager { get; set; }
        public bool IsWebApiEnabled { get; set; }
        public bool IsSchedulerEnabled { get; set; }
        public bool IsMessageQueueListenerManagerEnabled { get; set; }

        public EngineHostCompositionRoot()
        {}

        public EngineHostCompositionRoot(IList<Assembly> assembliesWithJobs)
            : this()
        {
            Container = new UnityContainer();
            IntegrationJobTypes = ExtractIntegrationJobTypesFromAssemblies(assembliesWithJobs);
        }

        public IList<Type> ExtractIntegrationJobTypesFromAssemblies(IList<Assembly> assembliesWithJobs)
        {
            return assembliesWithJobs
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(IIntegrationJob).IsAssignableFrom(x) && x.IsClass)
                .ToList();
        }

        public void Configure()
        {
            LoadConfiguration();
            SetupLogging();
            RegisterIntegrationPoints();
            RegisterIntegrationJobs();
            SetupRScriptRunner();
            SetupElasticsearchRepository();
            if (IsMessageQueueListenerManagerEnabled)
                SetupMessageQueueListenerManager();
            if (IsSchedulerEnabled)
                SetupEngineScheduler();
            if (IsWebApiEnabled)
                SetupWebApi();
        }

        public void LoadConfiguration()
        {
            try
            {
                new EngineConfiguration();
            }
            catch (Exception exception)
            {
                throw new Exception("Could not read configuration.", exception);
            }
            Container.RegisterType<IEngineConfiguration, EngineConfiguration>();
            EngineConfiguration = Container.Resolve<IEngineConfiguration>();
        }

        public void SetupLogging()
        {
            var config = EngineConfiguration.NLogAdapter;
            var properties = new NameValueCollection();
            properties["configType"] = config.ConfigType;
            properties["configFile"] = config.ConfigFile;
            Common.Logging.LogManager.Adapter = new NLogLoggerFactoryAdapter(properties);  
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void SetupDatabaseRepository(IntegrationEngineContext integrationEngineContext)
        {
            Container.RegisterInstance<IDatabaseRepository>(new DatabaseRepository(integrationEngineContext));
        }

        void RegisterConfig(Type from, Type to, string integrationPointName)
        {
            Container.RegisterType(from, to, integrationPointName,
                new InjectionConstructor(new ResolvedParameter<IEngineConfiguration>(), integrationPointName)
            );
        }

        public void RegisterIntegrationPoints()
        {
            Container.RegisterType<Elasticsearch.Net.Connection.IConnection, Elasticsearch.Net.Connection.HttpConnection>();
            Container.RegisterType<INestSerializer, NestSerializer>();
            Container.RegisterType<Elasticsearch.Net.Connection.ITransport, Elasticsearch.Net.Connection.Transport>();
            Container.RegisterType<IConnectionSettingsValues, ConnectionSettings>();
            foreach (var config in EngineConfiguration.IntegrationPoints.Mail) {
                RegisterConfig(typeof(IMailConfiguration), typeof(MailConfiguration), config.IntegrationPointName);
                Container.RegisterType<IMailClient, MailClient>(config.IntegrationPointName, new InjectionConstructor(config));
            }

            Func<IUnityContainer, Type, string, object> elasticClientFactory = (container, type, configName) => {
                var config = container.Resolve<IElasticsearchConfiguration>(configName);
                var serverUri = new UriBuilder(config.Protocol, config.HostName, config.Port).Uri;
                var settings = new ConnectionSettings(serverUri, config.DefaultIndex);
                return new ElasticClient(settings);
            };
            foreach (var config in EngineConfiguration.IntegrationPoints.Elasticsearch) {
                RegisterConfig(typeof(IElasticsearchConfiguration), typeof(ElasticsearchConfiguration), config.IntegrationPointName);
                Container.RegisterType<IElasticClient, ElasticClientAdapter>(config.IntegrationPointName, new InjectionFactory(elasticClientFactory));
            }
            foreach (var config in EngineConfiguration.IntegrationPoints.RabbitMQ) {
                RegisterConfig(typeof(IRabbitMQConfiguration), typeof(RabbitMQConfiguration), config.IntegrationPointName);
                Container.RegisterType<IRabbitMQClient, RabbitMQClient>(config.IntegrationPointName, new InjectionConstructor(config));
            }
            foreach (var config in EngineConfiguration.IntegrationPoints.JsonService) {
                RegisterConfig(typeof(IJsonServiceConfiguration), typeof(JsonServiceConfiguration), config.IntegrationPointName);
                Container.RegisterType<IJsonServiceClient, JsonServiceClientAdapter>(config.IntegrationPointName, new InjectionConstructor(config));
            }
        }

        /// <summary>
        /// Registers the integration jobs.
        /// Resolve the integration point type (specified in a job's parameters).
        /// Configure the integration point type with a configuration, based on a parameter's name.                    
        /// </summary>
        public void RegisterIntegrationJobs()
        {
            IntegrationJobTypes.ForEach(jobType => {
                Func<ParameterInfo[], object[]> resolveParameters = infos => {
                    var resolvedParameters = new List<object>();
                    foreach (var parameterInfo in infos)
                    {
                        var parameterType = parameterInfo.ParameterType; // The type of integration point (e.g. IElasticClient)
                        var parameterName = parameterInfo.ParameterType.Name; // The name of the configuration endpoint (e.g. "MyElasticClient")
                        // If the parameter implements IIntegrationPoint, resolve it's configuration type from the container.
                        if (typeof(IIntegrationPoint).IsAssignableFrom(parameterType))
                        {
                            var configType = parameterType.GetInterface(typeof(IIntegrationPoint<IIntegrationPointConfiguration>).Name).GetGenericArguments().Single(); ;
                            resolvedParameters.Add(Activator.CreateInstance(parameterType, Container.Resolve(configType, parameterName)));
                        }
                    }
                    return resolvedParameters.Cast<object>().ToArray();
                };
                var constructors = jobType.GetConstructors();
                if (constructors.Count() == 1 && !constructors.Single().GetParameters().Any()) // Handle Default Constructor case.
                    Container.RegisterType(jobType, new InjectionFactory((c, t, s) => Activator.CreateInstance(jobType)));
                else
                {
                    // Use the first constructor with parameters.
                    var constructor = constructors.First(x => x.GetParameters().Any());
                    Container.RegisterType(jobType, new InjectionFactory((c, t, s) => Activator.CreateInstance(jobType, resolveParameters(constructor.GetParameters())))); 
                }
            });
        }

        public async void SetupMessageQueueListenerManager()
        {
            var config = Container.Resolve<IRabbitMQConfiguration>("DefaultRabbitMQ");
            var messageQueueListenerFactory = new MessageQueueListenerFactory(Container, IntegrationJobTypes, config);
            MessageQueueListenerManager = new MessageQueueListenerManager() {
                MessageQueueListenerFactory = messageQueueListenerFactory,
            };
            await MessageQueueListenerManager.StartListener();
        }
            
        public void SetupEngineScheduler()
        {
            var dispatcher = new Dispatcher() {
                MessageQueueClient = Container.Resolve<IRabbitMQClient>("DefaultRabbitMQ"),
            };
            var engineScheduler = new EngineScheduler() {
                Scheduler = StdSchedulerFactory.GetDefaultScheduler(),
                IntegrationJobTypes = IntegrationJobTypes,
                Dispatcher = dispatcher,
            };
            Container.RegisterInstance<IEngineScheduler>(engineScheduler);
            var elasticsearchRepository = Container.Resolve<IElasticsearchRepository>();
            var engineSchedulerListener = new EngineSchedulerListener() {
                ElasticsearchRepository = elasticsearchRepository,
            };
            engineScheduler.AddSchedulerListener(engineSchedulerListener);
            engineScheduler.Start();
            var simpleTriggers = elasticsearchRepository.SelectAll<SimpleTrigger>();
            var allCronTriggers = elasticsearchRepository.SelectAll<CronTrigger>();
            var cronTriggers = allCronTriggers.Where(x => !string.IsNullOrWhiteSpace(x.CronExpressionString));
            foreach (var trigger in simpleTriggers)
                engineScheduler.ScheduleJobWithTrigger(trigger);
            foreach (var trigger in cronTriggers)
                engineScheduler.ScheduleJobWithTrigger(trigger);
            foreach(var cronTrigger in allCronTriggers.Where(x => string.IsNullOrWhiteSpace(x.CronExpressionString)))
                Log.Warn(x => x("Cron expression for trigger ({0}) is null, empty, or whitespace.", cronTrigger.Id));
        }

        public void SetupElasticsearchRepository()
        {
            Container.RegisterType<IElasticsearchRepository, ElasticsearchRepository>(new InjectionConstructor(new ResolvedParameter<IElasticClient>("DefaultElasticsearch")));
        }

        public void SetupRScriptRunner()
        {
            Container.RegisterInstance<IRScriptRunner>(new RScriptRunner());
        }

        public void SetupWebApi()
        {
            WebApiApplication = new WebApiApplication() { 
                WebApiConfiguration = EngineConfiguration.WebApi,
                ContainerResolver = new ContainerResolver(Container)
            };
            WebApiApplication.Start();
        }

        public void Dispose()
        {
            if (WebApiApplication != null)
                WebApiApplication.Dispose();
            if (MessageQueueListenerManager != null)
                MessageQueueListenerManager.Dispose();
        }
    }
}
