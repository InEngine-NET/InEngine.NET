using InEngine.Core;
using System;
using System.Threading.Tasks;

namespace InEngine.Commands.Sample;

public class SayHello : AbstractCommand
{
    public override async Task Run()
    {
        await Task.Run(() => Console.WriteLine("hello"));
    }
}