using System.Threading;
using InEngine.Core;

namespace InEngine.Commands.Sample;

public class Pause : AbstractCommand
{
    public override void Run() => Thread.Sleep(3000);
}