using System;
using InEngine.Core.IO;
using InEngine.Core.Queuing;
using InEngine.Core.Scheduling;

namespace InEngine.Core;

public class ServerHost : IDisposable, IHasMailSettings, IHasQueueSettings
{
    public SuperScheduler SuperScheduler { get; set; }
    public Dequeue Dequeue { get; set; }
    public MailSettings MailSettings { get; set; }
    public QueueSettings QueueSettings { get; set; }
    private bool isDisposed; 

    public void Start()
    {
        SuperScheduler = new SuperScheduler();
        SuperScheduler.Initialize(MailSettings);
        Dequeue = new Dequeue()
        {
            QueueSettings = QueueSettings,
            MailSettings = MailSettings,
        };

        SuperScheduler.Start();
        StartDequeueAsync();
    }

    public async void StartDequeueAsync() => await Dequeue.StartAsync();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (isDisposed) 
            return;
        if (!disposing) 
            return;
        SuperScheduler?.Shutdown();
        Dequeue.Dispose();
        Dequeue = null;
        isDisposed = true;
    }
}