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
        public ILog Log { get; set; }

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
//            var dbContext = SetupDatabaseContext();
            SetupRScriptRunner();
//            SetupDatabaseRepository();
//            var mailClient = SetupMailClient();
            var elasticClient = SetupElasticClient();
            var elasticsearchRepository = SetupElasticsearchRepository(elasticClient);
            var messageQueueClient = SetupMessageQueueClient();
            SetupEngineScheduler(messageQueueClient, elasticsearchRepository);
            SetupMessageQueueListener();
            SetupWebApi();
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
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void SetupDatabaseRepository(IntegrationEngineContext integrationEngineContext)
        {
            Container.RegisterInstance<IDatabaseRepository>(new DatabaseRepository(integrationEngineContext));
        }

        public void SetupWebApi()
        {
            var webApiApplication = new WebApiApplication() { 
                WebApiConfiguration = Configuration.WebApi
            };
            webApiApplication.Start();
            Container.RegisterInstance<IWebApiApplication>(webApiApplication);
        }

//        public IMailClient SetupMailClient()
//        {
//            var mailClient = new MailClient() {
//                MailConfiguration = Configuration.Mail
//            };
//            Container.RegisterInstance<IMailClient>(mailClient);
//            return mailClient;
//        }

        public void SetupMessageQueueListener()
        {
            var rabbitMqListener = new RabbitMQListener() {
                IntegrationJobTypes = IntegrationJobTypes,
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueue),
                MessageQueueConfiguration = Configuration.MessageQueue,
            };
            Container.RegisterInstance<IMessageQueueListener>(rabbitMqListener);
            rabbitMqListener.Listen();
        }

        public IMessageQueueClient SetupMessageQueueClient()
        {
            var messageQueueClient = new RabbitMQClient() {
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueue),
                MessageQueueConfiguration = Configuration.MessageQueue,
            };
            Container.RegisterInstance<IMessageQueueClient>(messageQueueClient);
            return messageQueueClient;
        }

        public void SetupEngineScheduler(IMessageQueueClient messageQueueClient, IElasticsearchRepository elasticsearchRepository)
        {
            var engineScheduler = new EngineScheduler() {
                Scheduler = StdSchedulerFactory.GetDefaultScheduler(),
                IntegrationJobTypes = IntegrationJobTypes,
                MessageQueueClient = messageQueueClient,
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
            var config = Configuration.Elasticsearch;
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
            var webApiApplication = Container.Resolve<IWebApiApplication>();
            webApiApplication.Stop();            
            var engineScheduler = Container.Resolve<IEngineScheduler>();
            engineScheduler.Shutdown();
            var messageQueueListener = Container.Resolve<IMessageQueueListener>();
            messageQueueListener.Dispose();
        }
    }
}

