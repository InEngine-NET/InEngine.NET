using System;
using InEngine.Core.Logging;
using InEngine.Core.Queuing;
using InEngine.Core.Scheduling;

namespace InEngine.Core
{
    public class ServerHost : IDisposable
    {
        public SuperScheduler SuperScheduler { get; set; }
        public Dequeue Dequeue { get; set; }

        public ServerHost() : this(null)
        {}

        public ServerHost(ILog log)
        {
            //SuperScheduler = new SuperScheduler();
            //SuperScheduler.Initialize();
            Dequeue = new Dequeue();
            if (log != null)
                Dequeue.Log = log;
        }

        public void Start()
        {
            //SuperScheduler.Start();
            StartDequeueAsync();
        }

        public async void StartDequeueAsync()
        {
            await Dequeue.StartAsync();
        }

        public void Dispose()
        {
            SuperScheduler?.Shutdown();
        }
    }
}
    