using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Reflection;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.R;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Model;
using Microsoft.Practices.Unity;
using Nest;
using Quartz;
using Quartz.Impl;
using log4net;
using log4net.Core;
using IntegrationEngine.Api;
using IntegrationEngine.Configuration;
using IntegrationEngine.MessageQueue;
using IntegrationEngine.Scheduler;

namespace IntegrationEngine
{
    public class EngineHostConfiguration
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
            TryAndLogFailure("Loading Configuration", LoadConfiguration);
            TryAndLogFailure("Setup Logging", SetupLogging);
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
            catch (Exception ex)
            {
                var log = LogManager.GetLogger(typeof(EngineHost));
                log.Error(description, ex);
            }
        }

        public void LoadConfiguration()
        {
            Configuration = new EngineConfiguration();
            Container.RegisterInstance<EngineConfiguration>(Configuration);
        }

        public void SetupLogging()
        {
            var log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
            var mailClient = new MailClient() {
                MailConfiguration = Configuration.Mail,
                Log = Container.Resolve<ILog>(),
            };

            Container.RegisterInstance<IMailClient>(mailClient);
        }

        public void SetupMessageQueueListener()
        {
            var rabbitMqListener = new RabbitMqListener() {
                IntegrationJobTypes = IntegrationJobTypes,
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueue),
                MessageQueueConfiguration = Configuration.MessageQueue,
                Log = Container.Resolve<ILog>(),
                MailClient = Container.Resolve<IMailClient>(),
                IntegrationEngineContext = Container.Resolve<IntegrationEngineContext>(),
                ElasticClient = Container.Resolve<IElasticClient>(),
            };
            rabbitMqListener.Listen();
        }

        public void SetupMessageQueueClient()
        {
            var messageQueueClient = new RabbitMqClient() {
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueue),
                MessageQueueConfiguration = Configuration.MessageQueue,
                Log = Container.Resolve<ILog>(),
            };
            Container.RegisterInstance<IMessageQueueClient>(messageQueueClient);
        }

        public void SetupScheduler()
        {
            var engineScheduler = new EngineScheduler() {
                Scheduler = StdSchedulerFactory.GetDefaultScheduler(),
                IntegrationJobTypes = IntegrationJobTypes,
                MessageQueueClient = Container.Resolve<IMessageQueueClient>(),
            };
            Container.RegisterInstance<IEngineScheduler>(engineScheduler);
            engineScheduler.Start();
            var simpleTriggers = Container.Resolve<IElasticClient>().Search<SimpleTrigger>(x => x).Documents;
            var cronTriggers = Container.Resolve<IElasticClient>().Search<CronTrigger>(x => x).Documents;
            foreach (var jobType in IntegrationJobTypes)
            {
                // Register job
                var jobDetail = engineScheduler.CreateJobDetail(jobType);                
                // Schedule the job with applicable triggers
                engineScheduler.ScheduleJobsWithSimpleTriggers(simpleTriggers, jobType, jobDetail);
                engineScheduler.ScheduleJobsWithCronTriggers(cronTriggers, jobType, jobDetail);
            }
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

            Container.RegisterInstance<IElasticClient>(elasticClient);
            Container.RegisterInstance<ESRepository<SimpleTrigger>>(new ESRepository<SimpleTrigger>() {
                ElasticClient = elasticClient
            });
            Container.RegisterInstance<ESRepository<CronTrigger>>(new ESRepository<CronTrigger>() {
                ElasticClient = elasticClient
            });
        }

        public void Shutdown()
        {
            var scheduler = Container.Resolve<IScheduler>();
            scheduler.Shutdown();
        }
    }
}

