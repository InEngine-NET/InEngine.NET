using System;
using System.Threading;
using Funq;
using Quartz;
using Quartz.API;
using Quartz.Impl;
using TryQuartz.Jobs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using TryQuartz.MessageQueue;

namespace TryQuartz
{
    public class ProgramConfiguration
    {
        public Container Container { get; set; }
        public ProgramConfiguration()
        {
        }

        public void Configure(Container container)
        {
            Container = container;
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

                // and start it off
                scheduler.Start();
                Console.WriteLine("Scheduler Started!");

                #region Create and schedule jobs
                // define the job and tie it to our HelloJob class
                IJobDetail job = JobBuilder.Create<RJob>()
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
                #endregion

                #region Setup Manager
                QuartzAPI.Configure(builder => {
                    //builder.UseScheduler(container.GetInstance<IScheduler>()); 
                    //This is the scheduler you are using, in this case I'm pulling out the scheduler from my structuremap container
                    builder.UseScheduler(scheduler);
                    builder.EnableCors();
                });
                QuartzAPI.Start("http://localhost:9001/");
                #endregion

                // Some sleep to show what's happening
                // This can be removed when run as a service.
                //Thread.Sleep(TimeSpan.FromSeconds(6000));

                // and last shut down the scheduler when you are ready to close your program
                //scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {
                Console.WriteLine(se);
            }
        }
    }
}

