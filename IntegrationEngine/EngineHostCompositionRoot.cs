using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.NLog;
using IntegrationEngine.Api;
using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.MessageQueue;
using IntegrationEngine.Core.R;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.MessageQueue;
using IntegrationEngine.Scheduler;
using Microsoft.Practices.Unity;
using Nest;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IntegrationEngine
{
    public class EngineHostCompositionRoot : IDisposable
    {
        public IUnityContainer Container { get; set; }
        public IEngineConfiguration EngineConfiguration { get; set; }
        public IList<Type> IntegrationJobTypes { get; set; }
        public ILog Log { get; set; }
        public IWebApiApplication WebApiApplication { get; set; }

        public EngineHostCompositionRoot()
        {}

        public void Configure(IList<Assembly> assembliesWithJobs)
        {
            Container = ContainerSingleton.GetContainer();
            IntegrationJobTypes = assembliesWithJobs
                        .SelectMany(x => x.GetTypes())
                        .Where(x => typeof(IIntegrationJob).IsAssignableFrom(x) && x.IsClass)
                        .ToList();
            LoadConfiguration();
            SetupLogging();
            RegisterIntegrationPoints();
            RegisterIntegrationJobs();
            SetupRScriptRunner();
            SetupElasticsearchRepository();
            SetupThreadedListenerManager();
            SetupEngineScheduler();
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

        public void RegisterIntegrationPoints()
        {
            Container.RegisterType<Elasticsearch.Net.Connection.IConnection, Elasticsearch.Net.Connection.HttpConnection>();
            Container.RegisterType<INestSerializer, NestSerializer>();
            Container.RegisterType<Elasticsearch.Net.Connection.ITransport, Elasticsearch.Net.Connection.Transport>();
            Container.RegisterType<IConnectionSettingsValues, ConnectionSettings>();
            //Container.RegisterType<IMailConfiguration, MailConfiguration>();
            foreach (var config in EngineConfiguration.IntegrationPoints.Mail) {
//                Container.RegisterInstance<IMailConfiguration>(config.IntegrationPointName, config);
                Container.RegisterType<IMailConfiguration, MailConfiguration>(config.IntegrationPointName, 
                    new InjectionConstructor(
                        new ResolvedParameter<IEngineConfiguration>(),
                        new ResolvedParameter<string>(config.IntegrationPointName)
                    )
                );
                Container.RegisterType<IMailClient, MailClient>(config.IntegrationPointName, new InjectionConstructor(config));
            }
            
            foreach (var config in EngineConfiguration.IntegrationPoints.Elasticsearch) {
                Container.RegisterInstance<IElasticsearchConfiguration>(config.IntegrationPointName, config);
                var serverUri = new UriBuilder(config.Protocol, config.HostName, config.Port).Uri;
                var settings = new ConnectionSettings(serverUri, config.DefaultIndex);
                var client = new ElasticClient(settings);
                Container.RegisterInstance<IElasticClient>(config.IntegrationPointName, client);
//                Container.RegisterType<IElasticClient, ElasticClient>(config.IntegrationPointName,
//                    new InjectionConstructor(
//                        settings,
//                        new ResolvedParameter<Elasticsearch.Net.Connection.IConnection>(),
//                        new ResolvedParameter<INestSerializer>(),
//                        new ResolvedParameter<Elasticsearch.Net.Connection.ITransport>())
//                );
            }
            foreach (var config in EngineConfiguration.IntegrationPoints.RabbitMQ) {
                Container.RegisterInstance<IRabbitMQConfiguration>(config.IntegrationPointName, config);
                Container.RegisterType<IMessageQueueClient, RabbitMQClient>(config.IntegrationPointName, new InjectionConstructor(config));
            }
        }

        public void RegisterIntegrationJobs()
        {
            foreach (var jobType in IntegrationJobTypes)
            {
                // Register the Types with the Container
                // Register a constructor with the Container
                // Need to know which constructor should be used.
                // If it has no constructor, assume default constructor
                // If it has one constructor, then use that constructor
                // If it has two or more constructors use the first one with arguments
                var constructorCount = jobType.GetConstructors().Count();
                if (constructorCount <= 1)
                {
                    Container.RegisterType(jobType);
                    continue;
                }
                //else if (constructorCount >= 2)
                //{
                var parameters = jobType.GetConstructors().Single(x => x.GetParameters().Any()).GetParameters();
                // Resolve the integration point type (in parameters).
                // Configure the integration point type with a configuration, based on the parameter name.                    
                var resolvedParameters = new List<ResolvedParameter>();
                foreach (var parameterInfo in parameters)
                {
                    var parameterType = parameterInfo.ParameterType;              // The type of integration point (e.g. IElasticClient)
                    var parameterName = parameterInfo.ParameterType.Name;         // The name of the configuration endpoint (e.g. "MyElasticClient")

                    if (typeof(IMailClient).IsAssignableFrom(parameterType))
                    {
                        // Register the integration point.
                        // To register the integration point, the parameters of the type's constructor must be known.
                        // To make this happen, an integration point must have a constructor that takes a configuration object parameter.
                        //var configName = parameterType.GetCustomAttribute<IntegrationPointConfigurationAttribute>().Name;
                        //var config = Container.Resolve<IMailConfiguration>(parameterName);
                        //Container.RegisterType(parameterType, new InjectionConstructor(config));

                        //Container.RegisterType<IMailClient, MailClient>(config.IntegrationPointName, new InjectionConstructor(config));
//                        Container.RegisterType(parameterType, typeof(MailClient));
                        resolvedParameters.Add(new ResolvedParameter(parameterType, parameterName));
                    }
                }
                var objectArray = resolvedParameters.Cast<object>().ToArray();
                Container.RegisterType(jobType, new InjectionConstructor(objectArray));
            }
        }

        public void SetupThreadedListenerManager()
        {
            var config = Container.Resolve<IRabbitMQConfiguration>("DefaultRabbitMQ");
            var rabbitMqListener = new RabbitMQListener() {
                IntegrationJobTypes = IntegrationJobTypes,
                MessageQueueConnection = new MessageQueueConnection(config),
                RabbitMQConfiguration = config,
            };

            var threadedListenerManager = new ThreadedListenerManager() {
                MessageQueueListener = rabbitMqListener,
            };
            Container.RegisterInstance<IThreadedListenerManager>(threadedListenerManager);
            threadedListenerManager.StartListener();
        }
            
        public void SetupEngineScheduler()
        {
            var dispatcher = new Dispatcher() {
                MessageQueueClient = Container.Resolve<IMessageQueueClient>("DefaultRabbitMQ"),
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
                WebApiConfiguration = EngineConfiguration.WebApi
            };
            WebApiApplication.Start();
        }

        public void Dispose()
        {
            if (WebApiApplication != null)
                WebApiApplication.Dispose();
        }
    }
}
