using System;
using System.Collections.Generic;
using System.IO;
using InEngine.Commands;
using InEngine.Core;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using InEngine.Core.Queuing;
using InEngine.Core.Queuing.Commands;              

namespace InEngine.IntegrationTest
{
    public class QueuingTest : ICommand
    {
        public void Run()
        {
            var queue = QueueAdapter.Make();

            queue.ClearPendingQueue();
            queue.Publish(new Echo() { VerbatimText = "Core echo command." });
            new Length { }.Run();
            new Peek { PendingQueue = true }.Run();
            new Consume { ShouldConsumeAll = true }.Run();

            Enqueue.Command(() => Console.WriteLine("Core lambda command."))
                   .Dispatch();
            Enqueue.Command(() => new Echo { VerbatimText = "Core echo command in a lambda command." }.Run())
                   .Dispatch();
            Enqueue.Command(new AlwaysFail())
                   .WriteOutputTo("queueWriteTest-TheFileShouldNotExist.txt")
                   .Dispatch();

            var queueWriteIntegrationTest = "queueWriteIntegrationTest.txt";
            var queueAppendIntegrationTest = "queueAppendIntegrationTest.txt";
            File.Delete(queueWriteIntegrationTest);
            File.Delete(queueAppendIntegrationTest);
            Enqueue.Command(new Echo { VerbatimText = "Core echo command in a lambda command." })
                   .PingAfter("http://www.google.com")
                   .PingBefore("http://www.google.com")
                   .EmailOutputTo("example@inengine.net")
                   .WriteOutputTo(queueWriteIntegrationTest)
                   .AppendOutputTo(queueAppendIntegrationTest)
                   .Dispatch();
            Enqueue.Commands(new[] {
                new Echo { VerbatimText = "Chain Link 1" },
                new Echo { VerbatimText = "Chain Link 2" },
            }).Dispatch();

            Enqueue.Commands(new List<AbstractCommand> {
                new Echo { VerbatimText = "Chain Link A" },
                new AlwaysFail(),
                new Echo { VerbatimText = "Chain Link C" },
            });

            new Consume { ShouldConsumeAll = true }.Run();
        }
    }
}
