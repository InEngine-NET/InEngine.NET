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
            SetupRScriptRunner();
            var elasticClient = SetupElasticClient();
            var elasticsearchRepository = SetupElasticsearchRepository(elasticClient);
            var messageQueueClient = SetupMessageQueueClient();
            var dispatcher = SetupDispatcher(messageQueueClient);
            SetupEngineScheduler(dispatcher, elasticsearchRepository);
            SetupThreadedListenerManager();
            SetupWebApi();
        }

        public void LoadConfiguration()
        {
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

        public void SetupWebApi()
        {
            WebApiApplication = new WebApiApplication() { 
                WebApiConfiguration = EngineConfiguration.WebApi
            };
            WebApiApplication.Start();
        }

        public void RegisterIntegrationPoints()
        {
            foreach (var config in EngineConfiguration.IntegrationPoints.Mail)
                Container.RegisterType<IMailClient, MailClient>(config.IntegrationPointName, new InjectionConstructor(config));
        }

        public void SetupThreadedListenerManager()
        {
            var rabbitMqListener = new RabbitMQListener() {
                IntegrationJobTypes = IntegrationJobTypes,
                MessageQueueConnection = new MessageQueueConnection(EngineConfiguration.MessageQueue),
                RabbitMQConfiguration = EngineConfiguration.MessageQueue,
            };

            var threadedListenerManager = new ThreadedListenerManager() {
                MessageQueueListener = rabbitMqListener,
            };
            Container.RegisterInstance<IThreadedListenerManager>(threadedListenerManager);
            threadedListenerManager.StartListener();
        }

        public IMessageQueueClient SetupMessageQueueClient()
        {
            var messageQueueClient = new RabbitMQClient() {
                MessageQueueConnection = new MessageQueueConnection(EngineConfiguration.MessageQueue),
                MessageQueueConfiguration = EngineConfiguration.MessageQueue,
            };
            Container.RegisterInstance<IMessageQueueClient>(messageQueueClient);
            return messageQueueClient;
        }

        public IDispatcher SetupDispatcher(IMessageQueueClient messageQueueClient)
        {
            return new Dispatcher() {
                MessageQueueClient = messageQueueClient,
            };
        }

        public void SetupEngineScheduler(IDispatcher dispatcher, IElasticsearchRepository elasticsearchRepository)
        {
            var engineScheduler = new EngineScheduler() {
                Scheduler = StdSchedulerFactory.GetDefaultScheduler(),
                IntegrationJobTypes = IntegrationJobTypes,
                Dispatcher = dispatcher,
            };
            Container.RegisterInstance<IEngineScheduler>(engineScheduler);
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

        public void SetupRScriptRunner()
        {
            Container.RegisterInstance<RScriptRunner>(new RScriptRunner());
        }

        public IElasticClient SetupElasticClient()
        {
            var config = EngineConfiguration.Elasticsearch;
            var serverUri = new UriBuilder(config.Protocol, config.HostName, config.Port).Uri;
            var settings = new ConnectionSettings(serverUri, config.DefaultIndex);
            var elasticClient = new ElasticClient(settings);
            Container.RegisterInstance<IElasticClient>(elasticClient);
            return elasticClient;
        }

        public IElasticsearchRepository SetupElasticsearchRepository(IElasticClient elasticClient)
        {
            var elasticsearchRepository = new ElasticsearchRepository() {
                ElasticClient = elasticClient,
            };
            if (!elasticsearchRepository.IsServerAvailable())
                Log.Warn("Elasticsearch server does not appear to be available.");
            Container.RegisterInstance<IElasticsearchRepository>(elasticsearchRepository);
            return elasticsearchRepository;
        }

        public void Dispose()
        {
            if (WebApiApplication != null)
                WebApiApplication.Dispose();
        }
    }
}
