using System;
using System.Collections.Generic;
using CommandLine;
using InEngine.Core;
using InEngine.Core.Commands;
using InEngine.Core.Scheduling;

namespace InEngine.Commands
{
    public class CommandsPlugin : AbstractPlugin
    {
        [VerbOption("fail", HelpText = "Always fail. Useful for end-to-end testing.")]
        public AlwaysFail AlwaysFail { get; set; }

        [VerbOption("succeed", HelpText = "A null operation command. Literally does nothing.")]
        public AlwaysSucceed Null { get; set; }

        public override void Schedule(ISchedule schedule)
        {
            schedule.Command(new Echo { VerbatimText = "Core Echo command." })
                    .EverySecond()
                    .Before(x => Console.WriteLine("Before"))
                    .After(x => Console.WriteLine("After"))
                    .PingBefore("http://www.google.com")
                    .PingAfter("http://www.google.com")
                    .WriteOutputTo("AlwaysSucceedWrite.log")
                    .AppendOutputTo("AlwaysSucceedAppend.log")
                    .EmailOutputTo("example@inengine.net");

            schedule.Command(new[] {
                new Echo { VerbatimText = "Chain Link 1" },
                new Echo { VerbatimText = "Chain Link 2" },
            }).EverySecond();

            schedule.Command(new List<AbstractCommand> {
                new Echo { VerbatimText = "Chain Link A" },
                new AlwaysFail(),
                new Echo { VerbatimText = "Chain Link C" },
            }).EverySecond();
        }
    }
}
