using System.Collections.Generic;
using CommandLine;
using InEngine.Core;
using InEngine.Core.Commands;
using InEngine.Core.Scheduling;

namespace InEngine.Commands;

public class CommandsPlugin : AbstractPlugin
{
    [VerbOption("fail", HelpText = "Always fail. Useful for end-to-end testing.")]
    public AlwaysFail AlwaysFail { get; set; }

    [VerbOption("succeed", HelpText = "A null operation command. Literally does nothing.")]
    public AlwaysSucceed Null { get; set; }

    public override void Schedule(ISchedule schedule)
    {
        schedule.Command(new Echo { VerbatimText = "Core Echo command." })
            .EveryMinute()
            .Before(x => x.Line($"Before {x.Name}"))
            .After(x => x.Line($"After {x.Name}"))
            .PingBefore("https://www.google.com")
            .PingAfter("https://www.google.com")
            .WriteOutputTo("AlwaysSucceedWrite.log")
            .AppendOutputTo("AlwaysSucceedAppend.log")
            .EmailOutputTo("example@inengine.net");

        schedule.Command(new AbstractCommand[] {
            new Echo { VerbatimText = "Chain Link 1" },
            new Echo { VerbatimText = "Chain Link 2" },
        }).EveryFiveMinutes();

        schedule.Command(new List<AbstractCommand> {
            new Echo { VerbatimText = "Chain Link A" },
            new AlwaysFail(),
            new Echo { VerbatimText = "Chain Link C" },
        }).EveryFifteenMinutes();
    }
}