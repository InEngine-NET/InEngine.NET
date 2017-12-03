using System;
using InEngine.Core;
using InEngine.Core.Commands;
using InEngine.Core.Queuing;
using InEngine.Core.Queuing.Commands;              

namespace InEngine.IntegrationTest
{
    public class QueuingTest : ICommand
    {
        public void Run()
        {
            var queue = Queue.Make();

            queue.ClearPendingQueue();
            queue.Publish(new Echo() { VerbatimText = "Core echo command." });
            new Length { }.Run();
            new Peek { PendingQueue = true }.Run();

            new Consume { ShouldConsumeAll = true }.Run();

            queue.ClearPendingQueue();
            queue.Publish(() => Console.WriteLine("Core lambda command."));

            new Length { }.Run();
            new Peek { PendingQueue = true }.Run();

            new Consume { ShouldConsumeAll = true }.Run();
        }
    }
}
