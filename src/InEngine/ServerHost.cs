using System;
using InEngine.Core.Scheduling;
using Quartz;
using Quartz.Impl;

namespace InEngine
{
    public class ServerHost : IDisposable
    {
        public Schedule Schedule { get; set; }

        public ServerHost()
        {
            Schedule = new Schedule();
            Common.Logging.LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter { Level = Common.Logging.LogLevel.Info };
            Jobs.Schedule(Schedule);
        }

        public void Dispose()
        {
            if (Schedule != null)
                Schedule.Shutdown();
        }

        public void Start()
        {
            Schedule.Start();
        }
    }
}
