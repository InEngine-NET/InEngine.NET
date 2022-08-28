using System.Threading;
using InEngine.Core;
using System.Threading.Tasks;

namespace InEngine.Commands.Sample;

public class Pause : AbstractCommand
{
    public override async Task Run()
    {
        await Task.Run(() => Thread.Sleep(3000));
    }
}