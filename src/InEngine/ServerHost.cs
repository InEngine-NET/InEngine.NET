using System;
using InEngine.Core.Queuing;
using InEngine.Core.Scheduling;
using Quartz;
using Quartz.Impl;

namespace InEngine
{
    public class ServerHost : IDisposable
    {
        public SuperScheduler SuperScheduler { get; set; }
        public Dequeue Dequeue { get; set; }

        public ServerHost()
        {
            SuperScheduler = new SuperScheduler();
            Common.Logging.LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter { Level = Common.Logging.LogLevel.Info };
            SuperScheduler.Initialize();
            Dequeue = new Dequeue();
        }

        public void Dispose()
        {
            SuperScheduler?.Shutdown();
        }

        public void Start()
        {
            SuperScheduler.Start();
            StartQueueServerAsync();
        }

        public async void StartQueueServerAsync()
        {
            await Dequeue.StartAsync();
        }
    }
}
    