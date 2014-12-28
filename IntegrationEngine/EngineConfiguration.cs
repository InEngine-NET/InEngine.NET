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

namespace IntegrationEngine
{
    public class EngineConfiguration
    {
        public Container Container { get; set; }
        public EngineConfiguration()
        {
        }

        public void Configure(Container container)
        {
            Container = container;
            SetupElasticClient();
            SetupRScriptRunner();
            SetupMessageQueueClient();
            SetupScheduler();
            SetupMessageQueueListener();
        }

        public void SetupMessageQueueListener()
        {
            RabbitMqListener.Listen();
        }

        public void SetupMessageQueueClient()
        {
            var messageQueueClient = new RabbitMqClient();
            Container.Register<IMessageQueueClient>(messageQueueClient);
        }

        public void SetupScheduler()
        {
            try
            {
                // Grab the Scheduler instance from the Factory 
                IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
                Container.Register<IScheduler>(scheduler);
                // and start it off
                scheduler.Start();
                Console.WriteLine("Scheduler Started!");
                // define the job and tie it to our HelloJob class
                IJobDetail job = JobBuilder.Create<CarReportJob>()
                    .WithIdentity("job1", "group1")
                    .Build();

                // Trigger the job to run now, and then repeat every 10 seconds
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity("trigger1")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(4).RepeatForever())
                    .Build();

                // Tell quartz to schedule the job using our trigger
                scheduler.ScheduleJob(job, trigger);
                // and last shut down the scheduler when you are ready to close your program
                //scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {
                Console.WriteLine(se);
            }
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

