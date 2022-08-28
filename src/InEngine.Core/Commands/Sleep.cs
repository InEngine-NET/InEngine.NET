using System.Threading;
using System.Threading.Tasks;

namespace InEngine.Core.Commands;

public class Sleep : AbstractCommand
{
    public int MillisecondsTimeout { get; set; }

    public override async Task Run()
    {
        Warning("Going to sleep...");
        Thread.Sleep(MillisecondsTimeout);
        await Task.Run(() => Info("Done sleeping!"));
    }
}