using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using InEngine.Core.IO;

namespace InEngine.Core.Queuing
{
    public class Dequeue : IDisposable
    {
        IList<QueueAdapter> queueAdapters;
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public QueueSettings QueueSettings { get; set; }
        public MailSettings MailSettings { get; set; }
        public ILog Log { get; set; } = LogManager.GetLogger<QueueAdapter>();

        public Dequeue()
        {
            queueAdapters = new List<QueueAdapter>();
            CancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            var allTasks = new List<Task>();
            Log.Debug("Start dequeue tasks for primary queue...");
            allTasks.AddRange(MakeTasks(true, QueueSettings.PrimaryQueueConsumers));
            Log.Debug("Start dequeue tasks for secondary queue...");
            allTasks.AddRange(MakeTasks(false, QueueSettings.SecondaryQueueConsumers));
            await Task.WhenAll(allTasks);

            // Recover from restart, if necessary.
            QueueAdapter.Make(false, QueueSettings, MailSettings).Recover();
            QueueAdapter.Make(true, QueueSettings, MailSettings).Recover();
        }

        IList<Task> MakeTasks(bool useSecondaryQueue = false, int numberOfTasks = 0)
        {
            return Enumerable.Range(0, numberOfTasks).Select((i) => {
                Log.Debug($"Registering Dequeuer #{i}");
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
            queueAdapters.ToList().ForEach(x => {
                if (x is IDisposable)
                    (x as IDisposable).Dispose();
            });
            CancellationTokenSource.Cancel();
        }
    }
}
