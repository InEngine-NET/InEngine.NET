using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.NLog;
using IntegrationEngine.Api;
using IntegrationEngine.Configuration;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Core.Mail;
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
    public class EngineHostConfiguration : IDisposable
    {
        public IUnityContainer Container { get; set; }
        public EngineConfiguration Configuration { get; set; }
        public IList<Type> IntegrationJobTypes { get; set; }

        public EngineHostConfiguration()
        {
        }

        public void Configure(IList<Assembly> assembliesWithJobs)
        {
            Container = ContainerSingleton.GetContainer();
            IntegrationJobTypes = assembliesWithJobs
                        .SelectMany(x => x.GetTypes())
                        .Where(x => typeof(IIntegrationJob).IsAssignableFrom(x) && x.IsClass)
                        .ToList();
            LoadConfiguration();
            var log = SetupLogging();
            var dbContext = SetupDatabaseContext();
            SetupRScriptRunner();
            SetupDatabaseRepository(dbContext);
            var mailClient = SetupMailClient(log);
            var elasticClient = SetupElasticClient();
            var elasticsearchRepository = SetupElasticsearchRepository(log, elasticClient);
            var messageQueueClient = SetupMessageQueueClient(log);
            SetupEngineScheduler(log, messageQueueClient, elasticsearchRepository);
            SetupMessageQueueListener(log, mailClient, elasticClient, dbContext);
            SetupWebApi();
        }

        public void LoadConfiguration()
        {
            Configuration = new EngineConfiguration();
            Container.RegisterInstance<EngineConfiguration>(Configuration);
        }

        public ILog SetupLogging()
        {
            var config = Configuration.NLogAdapter;
            var properties = new NameValueCollection();
            properties["configType"] = config.ConfigType;
            properties["configFile"] = config.ConfigFile;
            Common.Logging.LogManager.Adapter = new NLogLoggerFactoryAdapter(properties);  
            var log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            Container.RegisterInstance<ILog>(log);
            return log;
        }

        public IntegrationEngineContext SetupDatabaseContext()
        {
            var integrationEngineContext = new DatabaseInitializer(Configuration.Database).GetDbContext();
            Container.RegisterInstance<IntegrationEngineContext>(integrationEngineContext);
            return integrationEngineContext;
        }

        public void SetupDatabaseRepository(IntegrationEngineContext integrationEngineContext)
        {
            Container.RegisterInstance<IDatabaseRepository>(new DatabaseRepository(integrationEngineContext));
        }

        public void SetupWebApi()
        {
            var config = Configuration.WebApi;
            IntegrationEngineApi.Start((new UriBuilder("http", config.HostName, config.Port)).Uri.AbsoluteUri);
        }

        public IMailClient SetupMailClient(ILog log)
        {
            var mailClient = new MailClient() {
                MailConfiguration = Configuration.Mail,
                Log = log,
            };
            Container.RegisterInstance<IMailClient>(mailClient);
            return mailClient;
        }

        public void SetupMessageQueueListener(ILog log, IMailClient mailClient, IElasticClient elasticClient, IntegrationEngineContext integrationEngineContext)
        {
            var rabbitMqListener = new RabbitMQListener() {
                IntegrationJobTypes = IntegrationJobTypes,
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueue),
                MessageQueueConfiguration = Configuration.MessageQueue,
                Log = log,
                MailClient = mailClient,
                IntegrationEngineContext = integrationEngineContext,
                ElasticClient = elasticClient,
            };
            Container.RegisterInstance<IMessageQueueListener>(rabbitMqListener);
            rabbitMqListener.Listen();
        }

        public IMessageQueueClient SetupMessageQueueClient(ILog log)
        {
            var messageQueueClient = new RabbitMQClient() {
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueue),
                MessageQueueConfiguration = Configuration.MessageQueue,
                Log = log,
            };
            Container.RegisterInstance<IMessageQueueClient>(messageQueueClient);
            return messageQueueClient;
        }

        public void SetupEngineScheduler(ILog log, IMessageQueueClient messageQueueClient, IElasticsearchRepository elasticsearchRepository)
        {
            var engineScheduler = new EngineScheduler() {
                Scheduler = StdSchedulerFactory.GetDefaultScheduler(),
                IntegrationJobTypes = IntegrationJobTypes,
                MessageQueueClient = messageQueueClient,
                Log = log,
            };
            Container.RegisterInstance<IEngineScheduler>(engineScheduler);
            engineScheduler.Start();
            var simpleTriggers = elasticsearchRepository.SelectAll<SimpleTrigger>();
            var allCronTriggers = elasticsearchRepository.SelectAll<CronTrigger>();
            var cronTriggers = allCronTriggers.Where(x => !string.IsNullOrWhiteSpace(x.CronExpressionString));
            foreach (var trigger in simpleTriggers)
                engineScheduler.ScheduleJobWithTrigger(trigger);
            foreach (var trigger in cronTriggers)
                engineScheduler.ScheduleJobWithTrigger(trigger);
            foreach(var cronTrigger in allCronTriggers.Where(x => string.IsNullOrWhiteSpace(x.CronExpressionString)))
                log.Warn(x => x("Cron expression for trigger ({0}) is null, empty, or whitespace.", cronTrigger.Id));
        }

        public void SetupRScriptRunner()
        {
            Container.RegisterInstance<RScriptRunner>(new RScriptRunner());
        }

        public IElasticClient SetupElasticClient()
        {
            var config = Configuration.Elasticsearch;
            var serverUri = new UriBuilder(config.Protocol, config.HostName, config.Port).Uri;
            var settings = new ConnectionSettings(serverUri, config.DefaultIndex);
            var elasticClient = new ElasticClient(settings);
            Container.RegisterInstance<IElasticClient>(elasticClient);
            return elasticClient;
        }

        public IElasticsearchRepository SetupElasticsearchRepository(ILog log, IElasticClient elasticClient)
        {
            var elasticsearchRepository = new ElasticsearchRepository() {
                Log = log,
                ElasticClient = elasticClient,
            };
            if (!elasticsearchRepository.IsServerAvailable())
                log.Warn("Elasticsearch server does not appear to be available.");
            Container.RegisterInstance<IElasticsearchRepository>(elasticsearchRepository);
            return elasticsearchRepository;
        }

        public void Dispose()
        {
            var engineScheduler = Container.Resolve<IEngineScheduler>();
            engineScheduler.Shutdown();
            var messageQueueListener = Container.Resolve<IMessageQueueListener>();
            messageQueueListener.Dispose();
        }
    }
}

