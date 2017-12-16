using System;
using System.Collections.Generic;
using System.IO;
using InEngine.Commands;
using InEngine.Core;
using InEngine.Core.Commands;
using InEngine.Core.Scheduling;

namespace InEngine.IntegrationTest
{
    public class SchedulingTest : AbstractCommand
    {
        public override void Run()
        {
            var writingEverySecond = "writingEverySecond.txt";
            var appendingEverySecond = "appendingEverySecond.txt";
            File.Delete(writingEverySecond);
            File.Delete(appendingEverySecond);
            var superScheduler = new SuperScheduler();
            superScheduler.Initialize(InEngineSettings.Make().Mail);
            superScheduler.Schedule.Command(new Echo { VerbatimText = "Hello, world!" })
                    .EverySecond()
                    .Before(x => Console.WriteLine("Before"))
                    .After(x => Console.WriteLine("After"))
                    .PingBefore("http://www.google.com")
                    .PingAfter("http://www.google.com")
                    .WriteOutputTo(writingEverySecond)
                    .AppendOutputTo(appendingEverySecond)
                    .EmailOutputTo("example@inengine.net");

            superScheduler.Schedule.Command(new[] {
                new Echo { VerbatimText = "Chain Link 1" },
                new Echo { VerbatimText = "Chain Link 2" },
            }).EverySecond();

            superScheduler.Schedule.Command(new List<AbstractCommand> {
                new Echo { VerbatimText = "Chain Link A" },
                new AlwaysFail(),
                new Echo { VerbatimText = "Chain Link C" },
            }).EverySecond();

            superScheduler.Initialize();
            superScheduler.Start();
        }
    }
}
