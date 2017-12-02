using System;
using System.Linq.Expressions;
using InEngine.Commands;
using InEngine.Core.Commands;
using InEngine.Core.IO;
using InEngine.Core.Queuing;
using InEngine.Core.Queuing.Commands;              

namespace InEngine.IntegrationTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("Integration Tests");
            Console.WriteLine("------------------------------------------------------------");
            var queue = Queue.Make();

            queue.ClearPendingQueue();
            queue.Publish(new Echo() {VerbatimText = "Core echo command."});
            new Length { }.Run();
            new Peek { PendingQueue = true }.Run();

            new Consume { ShouldConsumeAll = true }.Run();

            queue.ClearPendingQueue();
            queue.Publish(() => Console.WriteLine("Core lambda command."));
            queue.Publish(() => Console.WriteLine("Core lambda command."));

            new Length { }.Run();
            new Peek { PendingQueue = true }.Run();

            new Consume { ShouldConsumeAll = true }.Run();
        }
    }
}
