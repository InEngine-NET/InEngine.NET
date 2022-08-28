using InEngine.Core;

namespace InEngine.Commands.Sample;

public class SayHello : AbstractCommand
{
    public override void Run() => Line("hello");
}