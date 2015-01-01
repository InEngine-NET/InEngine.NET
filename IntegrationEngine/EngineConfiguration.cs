using IntegrationEngine.Jobs;
using IntegrationEngine.Mail;
using IntegrationEngine.MessageQueue;
using IntegrationEngine.R;
using IntegrationEngine.Storage;
using log4net;
using MySql.Data.Entity;
using MySql.Data.MySqlClient;
using Nest;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
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
            DbConfiguration.SetConfiguration(new MySqlEFConfiguration());
            var dbConfig = Configuration.DatabaseConfiguration;
            var connectionStringBuilder = new MySqlConnectionStringBuilder() {
                Server = dbConfig.HostName,
                Port = dbConfig.Port,
                Database = dbConfig.DatabaseName,
                UserID = dbConfig.UserName,
                Password = dbConfig.Password,
            };
            var dbContext = new IntegrationEngineContext(connectionStringBuilder.ConnectionString);
            dbContext.Database.CreateIfNotExists();
            var repository = new Repository<IntegrationEngine.Models.MailMessage>(dbContext);
        }

        public void SetupMailClient()
        {
            var mailClient = new MailClient() {
                MailConfiguration = Configuration.MailConfiguration,
            };
            Container.Register<IMailClient>(mailClient);
        }

        public void SetupMessageQueueListener(IList<Assembly> assembliesWithJobs)
        {
            var rabbitMqListener = new RabbitMqListener() {
                AssembliesWithJobs = assembliesWithJobs,
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueueConfiguration),
                MessageQueueConfiguration = Configuration.MessageQueueConfiguration,
            };
            rabbitMqListener.Listen();
        }

        public void SetupMessageQueueClient()
        {
            var messageQueueClient = new RabbitMqClient() {
                MessageQueueConnection = new MessageQueueConnection(Configuration.MessageQueueConfiguration),
                MessageQueueConfiguration = Configuration.MessageQueueConfiguration,
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

            foreach(var jobType in jobTypes) 
            {
                var integrationJob = Activator.CreateInstance(jobType) as IIntegrationJob;
                var jobDetailsDataMap = new JobDataMap();
                jobDetailsDataMap.Put("IntegrationJob", integrationJob);
                var jobDetail = JobBuilder.Create<MessageQueueJob>()
                    .SetJobData(jobDetailsDataMap)
                    .WithIdentity(jobType.Name, jobType.Namespace)
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .WithIdentity("trigger-" + jobType.Name, jobType.Namespace);
                if (!object.Equals(integrationJob.Interval, default(TimeSpan)))
                    trigger.WithSimpleSchedule(x => x
                        .WithInterval(integrationJob.Interval)
                        .RepeatForever());
                if (object.Equals(integrationJob.StartTimeUtc, default(DateTimeOffset)))
                    trigger.StartNow();
                else 
                    trigger.StartAt(integrationJob.StartTimeUtc);
                scheduler.ScheduleJob(jobDetail, trigger.Build());
            }
        }
            
        public void SetupRScriptRunner()
        {
            Container.Register<RScriptRunner>(new RScriptRunner());
        }

        public void SetupElasticClient()
        {
            var node = new Uri("http://localhost:9200");
            var settings = new ConnectionSettings(
                node, 
                defaultIndex: "my-application"
            );
            Container.Register<IElasticClient>(new ElasticClient(settings));
        }

        public void Shutdown()
        {
            var scheduler = Container.Resolve<IScheduler>();
            scheduler.Shutdown();
        }
    }
}

