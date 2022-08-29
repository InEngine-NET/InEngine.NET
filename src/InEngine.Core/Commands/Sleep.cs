using System.Threading;
using System.Threading.Tasks;

namespace InEngine.Core.Commands;

using CommandLine;

public class Sleep : AbstractCommand
{
    [Option("duration", HelpText = "The number of seconds to sleep.")]
    public int DurationInSeconds { get; set; } = 3;

    public override async Task RunAsync()
    {
        await WarningAsync("Going to sleep...");
        Thread.Sleep(DurationInSeconds * 1000);
        await InfoAsync("Done sleeping!");
    }
}