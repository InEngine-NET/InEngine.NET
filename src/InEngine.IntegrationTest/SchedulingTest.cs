using System;
using System.IO;
using InEngine.Core;
using InEngine.Core.Commands;
using InEngine.Core.Scheduling;

namespace InEngine.IntegrationTest
{
    public class SchedulingTest : ICommand
    {
        public void Run()
        {
            var writingEverySecond = "writingEverySecond";
            var appendingEverySecond = "appendingEverySecond";
            File.Delete(writingEverySecond);
            File.Delete(appendingEverySecond);
            var schedule = new Schedule();
            schedule.Job(new Echo { VerbatimText = "Echo this text every seconds" }).EverySecond();
            schedule.Job(new Echo { VerbatimText = "Echo this text every seconds" })
                    .EverySecond()
                    .Before(x => Console.WriteLine("Before"))
                    .After(x => Console.WriteLine("Before"))
                    .PingBefore("http://www.google.com")
                    .PingAfter("http://www.google.com")
                    .WriteOutputTo(writingEverySecond)
                    .AppendOutputTo(appendingEverySecond);

            schedule.Initialize();
            schedule.Start();
        }
    }
}
