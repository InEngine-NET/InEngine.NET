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
        public Dequeue ConsumeServer { get; set; }

        public ServerHost()
        {
            SuperScheduler = new SuperScheduler();
            Common.Logging.LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter { Level = Common.Logging.LogLevel.Info };
            SuperScheduler.Initialize();
            ConsumeServer = new Dequeue() {
                TaskCount = 10
            };
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
            await ConsumeServer.StartAsync();
        }
    }
}
    