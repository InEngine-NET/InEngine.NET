using System;
using Quartz;
using Quartz.Impl;

namespace InEngine
{
    public class ServerHost : IDisposable
    {
        public IScheduler Scheduler { get; set; }

        public ServerHost()
        {
            Scheduler = StdSchedulerFactory.GetDefaultScheduler();
            Common.Logging.LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter { Level = Common.Logging.LogLevel.Info };
            Jobs.Schedule(Scheduler);
        }

        public void Dispose()
        {
            if (Scheduler != null && Scheduler.IsStarted)
                Scheduler.Shutdown();
        }

        public void Start()
        {
            Scheduler.Start();
        }
    }
}
