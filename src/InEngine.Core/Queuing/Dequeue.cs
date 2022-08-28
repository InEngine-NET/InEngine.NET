﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InEngine.Core.IO;
using Microsoft.Extensions.Logging;

namespace InEngine.Core.Queuing;

public class Dequeue : IDisposable
{
    private readonly IList<QueueAdapter> queueAdapters;
    private bool isDisposed;
    public CancellationTokenSource CancellationTokenSource { get; set; }
    public QueueSettings QueueSettings { get; set; }
    public MailSettings MailSettings { get; set; }
    public ILogger Log { get; set; } = LogManager.GetLogger<QueueAdapter>();

    public Dequeue()
    {
        queueAdapters = new List<QueueAdapter>();
        CancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartAsync()
    {
        var allTasks = new List<Task>();
        Log.LogDebug("Start dequeue tasks for primary queue...");
        allTasks.AddRange(MakeTasks(true, QueueSettings.PrimaryQueueConsumers));
        Log.LogDebug("Start dequeue tasks for secondary queue...");
        allTasks.AddRange(MakeTasks(false, QueueSettings.SecondaryQueueConsumers));
        await Task.WhenAll(allTasks);

        // Recover from restart, if necessary.
        QueueAdapter.Make(false, QueueSettings, MailSettings).Recover();
        QueueAdapter.Make(true, QueueSettings, MailSettings).Recover();
    }

    IList<Task> MakeTasks(bool useSecondaryQueue = false, int numberOfTasks = 0)
    {
        return Enumerable.Range(0, numberOfTasks).Select((i) => {
            Log.LogDebug("Registering Dequeuer {I}", i);
            return Task.Factory.StartNew(() => {
                var queue = QueueAdapter.Make(useSecondaryQueue, QueueSettings, MailSettings);
                queue.Id = i;
                queueAdapters.Add(queue);
                queue.Consume(CancellationTokenSource.Token);
            }, TaskCreationOptions.LongRunning);
        }).ToList();
    }

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
        queueAdapters.ToList().ForEach(x => ((IDisposable)x)?.Dispose());
        CancellationTokenSource.Cancel();
        isDisposed = true;
    }
}