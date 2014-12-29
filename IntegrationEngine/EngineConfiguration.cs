using System;
using System.Threading;
using Funq;
using Quartz;
using Quartz.API;
using Quartz.Impl;
using IntegrationEngine.Jobs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using IntegrationEngine.MessageQueue;
using Nest;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace IntegrationEngine
{
    public class EngineConfiguration
    {
        public Container Container { get; set; }
        public EngineConfiguration()
        {
        }

        public void Configure(Container container, IList<Assembly> assembliesWithJobs)
        {
            Container = container;
            SetupElasticClient();
            SetupRScriptRunner();
            SetupMessageQueueClient();
            SetupScheduler(assembliesWithJobs);
            SetupMessageQueueListener(assembliesWithJobs);
        }

        public void SetupMessageQueueListener(IList<Assembly> assembliesWithJobs)
        {
            RabbitMqListener.Listen(assembliesWithJobs);
        }

        public void SetupMessageQueueClient()
        {
            var messageQueueClient = new RabbitMqClient();
            Container.Register<IMessageQueueClient>(messageQueueClient);
        }

        public void SetupScheduler(IList<Assembly> assembliesWithJobs)
        {
            IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
            Container.Register<IScheduler>(scheduler);
            scheduler.Start();
            Console.WriteLine("Scheduler Started!");

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
            // Shutdown scheduler
            //scheduler.Shutdown();
        }

        public void SetupSchedulerApi()
        {
            QuartzAPI.Configure(builder => {
                builder.UseScheduler(Container.Resolve<IScheduler>());
                builder.EnableCors();
            });
            QuartzAPI.Start("http://localhost:9001/");
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
    }
}

