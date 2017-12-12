using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InEngine.Core.Queuing
{
    public class Dequeue : IDisposable
    {
        IList<QueueAdapter> queueAdapters;
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public QueueSettings QueueSettings { get; set; }

        public Dequeue()
        {
            queueAdapters = new List<QueueAdapter>();
            CancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            if (QueueSettings == null)
                QueueSettings = InEngineSettings.Make().Queue;
            
            // Create dequeue tasks for primary and secondary queues.
            var allTasks = new List<Task>();
            Console.WriteLine("Start dequeue tasks for primary queue...");
            allTasks.AddRange(MakeTasks(true, QueueSettings.PrimaryQueueConsumers));
            Console.WriteLine("Start dequeue tasks for secondary queue...");
            allTasks.AddRange(MakeTasks(false, QueueSettings.SecondaryQueueConsumers));
            await Task.WhenAll(allTasks);

            // Recover from restart, if necessary.
            QueueAdapter.Make().Recover();
            QueueAdapter.Make(true).Recover();
        }

        IList<Task> MakeTasks(bool useSecondaryQueue = false, int numberOfTasks = 0)
        {
            return Enumerable.Range(0, numberOfTasks).Select((i) => {
                Console.WriteLine($"Registering Dequeuer #{i}");
                return Task.Factory.StartNew(() => {
                    var queue = QueueAdapter.Make(useSecondaryQueue, QueueSettings);
                    queue.Id = i;
                    queueAdapters.Add(queue);
                    queue.Consume(CancellationTokenSource.Token);
                }, TaskCreationOptions.LongRunning);
            }).ToList();
        }

        public void Dispose()
        {
            CancellationTokenSource.Cancel();
        }
    }
}
