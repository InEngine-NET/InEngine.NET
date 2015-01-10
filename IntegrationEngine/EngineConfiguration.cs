using IntegrationEngine.Api;
using IntegrationEngine.Configuration;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.R;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.MessageQueue;
using IntegrationEngine.Model;
using log4net;
using Nest;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IntegrationEngine
{
    public class EngineConfiguration
    {
        public EngineJsonConfiguration Configuration { get; set; }
        public EngineConfiguration()
        {
        }

        public void Configure(IList<Assembly> assembliesWithJobs)
        {
            LoadConfiguration();
            SetupLogging();
            SetupDatabaseRepository();
            SetupMailClient();
            SetupElasticClient();
            SetupRScriptRunner();
            SetupMessageQueueClient();
            SetupScheduler(assembliesWithJobs);
            SetupApi();
            SetupMessageQueueListener(assembliesWithJobs);
        }

        public void LoadConfiguration()
        {
            Configuration = new EngineJsonConfiguration();
            Container.Register<EngineJsonConfiguration>(Configuration);
        }

        public void SetupLogging()
        {
            var log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            Container.Register<ILog>(log);
        }

        public void SetupDatabaseRepository()
        {
            var dbContext = new DatabaseInitializer(Configuration.Database).GetDbContext();
            Container.Register<IntegrationEngineContext>(dbContext);
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
            };
            Container.Register<IMailClient>(mailClient);
        }

        public void SetupMessageQueueListener(IList<Assembly> assembliesWithJobs)
        {
            var rabbitMqListener = new RabbitMqListener() {
                AssembliesWithJobs = assembliesWithJobs,
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueue),
                MessageQueueConfiguration = Configuration.MessageQueue,
            };
            rabbitMqListener.Listen();
        }

        public void SetupMessageQueueClient()
        {
            var messageQueueClient = new RabbitMqClient() {
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueue),
                MessageQueueConfiguration = Configuration.MessageQueue,
            };
            Container.Register<IMessageQueueClient>(messageQueueClient);
        }

        public void SetupScheduler(IList<Assembly> assembliesWithJobs)
        {
            IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
            Container.Register<IScheduler>(scheduler);
            scheduler.Start();
            var jobTypes = assembliesWithJobs
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(IIntegrationJob).IsAssignableFrom(x) && x.IsClass);
            var integrationJobs = Container.Resolve<IElasticClient>().Search<IntegrationJob>(x => x).Documents;
            foreach (var jobType in jobTypes)
            {
                foreach (var schedule in integrationJobs.Where(x => x.JobType == jobType.FullName))
                {
                    var scheduleId = string.Format("{0}-{1}", jobType.Name, schedule.Id);
                    var integrationJob = Activator.CreateInstance(jobType) as IIntegrationJob;
                    var jobDetailsDataMap = new JobDataMap();
                    jobDetailsDataMap.Put("IntegrationJob", integrationJob);
                    var jobDetail = JobBuilder.Create<IntegrationJobDispatcherJob>()
                        .SetJobData(jobDetailsDataMap)
                        .WithIdentity(scheduleId, jobType.Namespace)
                        .Build();
                    var trigger = TriggerBuilder.Create()
                        .WithIdentity("trigger-" + scheduleId, jobType.Namespace);
                    if (schedule.IntervalTicks > 0)
                        trigger.WithSimpleSchedule(x => x
                            .WithInterval(new TimeSpan(schedule.IntervalTicks))
                            .RepeatForever());
                    if (!object.Equals(schedule.StartTimeUtc, default(DateTimeOffset)))
                        trigger.StartAt(schedule.StartTimeUtc);
                    else
                        trigger.StartNow();
                    scheduler.ScheduleJob(jobDetail, trigger.Build());
                }
            }
        }

        public void SetupRScriptRunner()
        {
            Container.Register<RScriptRunner>(new RScriptRunner());
        }

        public void SetupElasticClient()
        {
            var config = Configuration.Elasticsearch;
            var serverUri = new UriBuilder(config.Protocol, config.HostName, config.Port).Uri;
            var settings = new ConnectionSettings(serverUri, config.DefaultIndex);
            var elasticClient = new ElasticClient(settings);
            Container.Register<IElasticClient>(elasticClient);
            var repository = new ESRepository<IntegrationJob>() {
                ElasticClient = elasticClient
            };
            Container.Register<ESRepository<IntegrationJob>>(repository);
        }

        public void Shutdown()
        {
            var scheduler = Container.Resolve<IScheduler>();
            scheduler.Shutdown();
        }
    }
}

