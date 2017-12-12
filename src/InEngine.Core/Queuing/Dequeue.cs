using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InEngine.Core.Queuing
{
    public class Dequeue
    {
        IList<QueueAdapter> queueAdapters;
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public int TaskCount { get; set; }

        public Dequeue()
        {
            queueAdapters = new List<QueueAdapter>();
            CancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            // Create dequeue tasks for primary and secondary queues.
            var allTasks = new List<Task>();
            Console.WriteLine("Start dequeue tasks for primary queue...");
            allTasks.AddRange(MakeTasks());
            Console.WriteLine("Start dequeue tasks for secondary queue...");
            allTasks.AddRange(MakeTasks());
            await Task.WhenAll(allTasks);

            // Recover from restart, if necessary.
            QueueAdapter.Make().Recover();
            QueueAdapter.Make(true).Recover();
        }

        public IList<Task> MakeTasks()
        {
            return Enumerable.Range(0, TaskCount).Select((i) => {
                Console.WriteLine($"Registering Dequeuer #{i}");
                return Task.Factory.StartNew(() => {
                    var queue = QueueAdapter.Make();
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
