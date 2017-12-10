using System;
using InEngine.Core.Scheduling;
using Quartz;
using Quartz.Impl;

namespace InEngine
{
    public class ServerHost : IDisposable
    {
        public SuperScheduler SuperScheduler { get; set; }

        public ServerHost()
        {
            SuperScheduler = new SuperScheduler();
            Common.Logging.LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter { Level = Common.Logging.LogLevel.Info };
            SuperScheduler.Initialize();
        }

        public void Dispose()
        {
            SuperScheduler?.Shutdown();
        }

        public void Start()
        {
            SuperScheduler.Start();
        }
    }
}
