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
            SetupLogging();
            TryAndLogFailure("Setup Database Repository", SetupDatabaseRepository);
            TryAndLogFailure("Setup Mail Client", SetupMailClient);
            TryAndLogFailure("Setup Elastic Client", SetupElasticClient);
            TryAndLogFailure("Setup RScript Runner", SetupRScriptRunner);
            TryAndLogFailure("Setup Message Queue Client", SetupMessageQueueClient);
            TryAndLogFailure("Setup Scheduler", SetupScheduler);
            TryAndLogFailure("Setup Web Api", SetupApi);
            TryAndLogFailure("Setup Message Queue Listener", SetupMessageQueueListener);
        }

        static void TryAndLogFailure(string description, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                var log = Common.Logging.LogManager.GetLogger(typeof(EngineHost));
                log.Error(description, exception);
            }
        }

        public void LoadConfiguration()
        {
            Configuration = new EngineConfiguration();
            Container.RegisterInstance<EngineConfiguration>(Configuration);
        }

        public void SetupLogging()
        {
            var config = Configuration.NLogAdapter;
            var properties = new NameValueCollection();
            properties["configType"] = config.ConfigType;
            properties["configFile"] = config.ConfigFile;
            Common.Logging.LogManager.Adapter = new NLogLoggerFactoryAdapter(properties);  
            var log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            Container.RegisterInstance<ILog>(log);
        }

        public void SetupDatabaseRepository()
        {
            Container.RegisterInstance<IntegrationEngineContext>(new DatabaseInitializer(Configuration.Database).GetDbContext());
        }

        public void SetupApi()
        {
            var config = Configuration.WebApi;
            IntegrationEngineApi.Start((new UriBuilder("http", config.HostName, config.Port)).Uri.AbsoluteUri);
        }

        public void SetupMailClient()
        {
            var log = Container.Resolve<ILog>();

            var mailClient = new MailClient() {
                MailConfiguration = Configuration.Mail,
                Log = log,
            };
            mailClient.IsServerAvailable();
            Container.RegisterInstance<IMailClient>(mailClient);
        }

        public void SetupMessageQueueListener()
        {
            var rabbitMqListener = new RabbitMQListener() {
                IntegrationJobTypes = IntegrationJobTypes,
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueue),
                MessageQueueConfiguration = Configuration.MessageQueue,
                Log = Container.Resolve<ILog>(),
                MailClient = Container.Resolve<IMailClient>(),
                IntegrationEngineContext = Container.Resolve<IntegrationEngineContext>(),
                ElasticClient = Container.Resolve<IElasticClient>(),
            };
            Container.RegisterInstance<IMessageQueueListener>(rabbitMqListener);
            rabbitMqListener.Listen();
        }

        public void SetupMessageQueueClient()
        {
            var messageQueueClient = new RabbitMQClient() {
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueue),
                MessageQueueConfiguration = Configuration.MessageQueue,
                Log = Container.Resolve<ILog>(),
            };
            Container.RegisterInstance<IMessageQueueClient>(messageQueueClient);
        }

        public void SetupScheduler()
        {
            var log = Container.Resolve<ILog>();
            var engineScheduler = new EngineScheduler() {
                Scheduler = StdSchedulerFactory.GetDefaultScheduler(),
                IntegrationJobTypes = IntegrationJobTypes,
                MessageQueueClient = Container.Resolve<IMessageQueueClient>(),
                Log = log,
            };
            Container.RegisterInstance<IEngineScheduler>(engineScheduler);
            engineScheduler.Start();

            var elasticRepo = Container.Resolve<IElasticsearchRepository>();
            var simpleTriggers = elasticRepo.SelectAll<SimpleTrigger>();
            var allCronTriggers = elasticRepo.SelectAll<CronTrigger>();
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

        public void SetupElasticClient()
        {
            var config = Configuration.Elasticsearch;
            var serverUri = new UriBuilder(config.Protocol, config.HostName, config.Port).Uri;
            var settings = new ConnectionSettings(serverUri, config.DefaultIndex);
            var elasticClient = new ElasticClient(settings);
            var log = Container.Resolve<ILog>();
            Container.RegisterInstance<IElasticClient>(elasticClient);
            var elasticRepo = new ElasticsearchRepository() {
                ElasticClient = elasticClient,
                Log = log,
            };
            if (!elasticRepo.IsServerAvailable())
                log.Warn("Elasticsearch server does not appear to be available.");
            Container.RegisterInstance<IElasticsearchRepository>(elasticRepo);
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

